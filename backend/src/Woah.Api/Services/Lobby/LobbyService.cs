using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts;
using Woah.Api.Contracts.Lobbies;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Notifications;

namespace Woah.Api.Services.Lobby;

public class LobbyService : ILobbyService
{
    private readonly WoahDbContext _dbContext;
    private readonly ILobbyCodeGenerator _codeGenerator;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LobbyService> _logger;

    public LobbyService(
        WoahDbContext dbContext,
        ILobbyCodeGenerator codeGenerator,
        IGameNotifier notifier,
        TimeProvider timeProvider,
        ILogger<LobbyService> logger)
    {
        _dbContext = dbContext;
        _codeGenerator = codeGenerator;
        _notifier = notifier;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CreateLobbyResponse> CreateLobbyAsync(CreateLobbyRequest request, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var nick = request.HostNick.Trim();
        var code = await GenerateUniqueCodeAsync(ct);

        var host = new PlayerEntity { PlayerId = Guid.NewGuid(), Nick = nick, CreatedAt = now };

        var playlist = new PlaylistEntity
        {
            PlaylistId = Guid.NewGuid(),
            OwnerPlayerId = host.PlayerId,
            Name = $"Lobby {code}",
            CreatedAt = now
        };

        var lobby = new LobbyEntity
        {
            LobbyId = Guid.NewGuid(),
            Code = code,
            Status = LobbyStatus.Waiting,
            CreatedAt = now,
            HostPlayerId = host.PlayerId,
            MaxPlayers = request.MaxPlayers,
            ActivePlaylistId = playlist.PlaylistId
        };

        var membership = new LobbyPlayerEntity
        {
            LobbyId = lobby.LobbyId,
            PlayerId = host.PlayerId,
            Nick = nick,
            JoinedAt = now
        };

        _dbContext.Players.Add(host);
        _dbContext.Playlists.Add(playlist);
        _dbContext.Lobbies.Add(lobby);
        _dbContext.LobbyPlayers.Add(membership);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Lobby {LobbyCode} created by {Nick} (HostPlayerId={HostPlayerId})", code, nick, host.PlayerId);

        return new CreateLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            HostPlayerId = host.PlayerId,
            PlaylistId = playlist.PlaylistId
        };
    }

    public async Task<JoinLobbyResponse> JoinLobbyAsync(string lobbyCode, JoinLobbyRequest request, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var nick = request.Nick.Trim();
        var normalizedCode = lobbyCode.NormalizeCode();

        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

        LobbyEntity lobby;
        PlayerEntity player;

        try
        {
            lobby = await _dbContext.GetLobbyWithPlayersForUpdateAsync(normalizedCode, ct);

            if (lobby.Status != LobbyStatus.Waiting)
            {
                _logger.LogWarning("Join rejected — lobby {LobbyCode} is not waiting (Status={Status})", lobby.Code, lobby.Status);
                throw new BadRequestException("Lobby is not accepting new players.");
            }

            var active = lobby.ActivePlayers();

            if (active.Count >= lobby.MaxPlayers)
            {
                _logger.LogWarning("Join rejected — lobby {LobbyCode} is full ({Count}/{Max})", lobby.Code, active.Count, lobby.MaxPlayers);
                throw new BadRequestException("Lobby is full.");
            }

            if (active.Any(x => string.Equals(x.Nick, nick, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Join rejected — nick {Nick} already taken in lobby {LobbyCode}", nick, lobby.Code);
                throw new BadRequestException("Nick is already taken in this lobby.");
            }

            player = new PlayerEntity { PlayerId = Guid.NewGuid(), Nick = nick, CreatedAt = now };
            var membership = new LobbyPlayerEntity
            {
                LobbyId = lobby.LobbyId,
                PlayerId = player.PlayerId,
                Nick = nick,
                JoinedAt = now
            };

            _dbContext.Players.Add(player);
            _dbContext.LobbyPlayers.Add(membership);
            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        _logger.LogInformation("Player {Nick} (PlayerId={PlayerId}) joined lobby {LobbyCode}", nick, player.PlayerId, lobby.Code);
        await _notifier.LobbyUpdated(lobby.Code);

        return new JoinLobbyResponse
        {
            PlayerId = player.PlayerId,
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            Nick = nick
        };
    }

    public async Task<GetLobbyResponse> GetLobbyAsync(string lobbyCode, CancellationToken ct = default)
    {
        var lobby = await GetLobbyWithPlayersAsync(lobbyCode.NormalizeCode(), ct);
        var active = lobby.ActivePlayers();

        var sessionId = await _dbContext.GameSessions
            .Where(x => x.LobbyId == lobby.LobbyId && x.EndedAt == null)
            .Select(x => (Guid?)x.SessionId)
            .FirstOrDefaultAsync(ct);

        return new GetLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            Code = lobby.Code,
            Status = lobby.Status.ToContract(),
            MaxPlayers = lobby.MaxPlayers,
            HostPlayerId = lobby.HostPlayerId,
            PlayerCount = active.Count,
            CurrentSessionId = sessionId,
            ActivePlaylistId = lobby.ActivePlaylistId,
            Players = active.Select(x => new LobbyPlayerResponse
            {
                PlayerId = x.PlayerId,
                Nick = x.Nick,
                JoinedAt = x.JoinedAt,
                IsHost = x.PlayerId == lobby.HostPlayerId
            }).ToList()
        };
    }

    public async Task<LeaveLobbyResponse> LeaveLobbyAsync(string lobbyCode, LeaveLobbyRequest request, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var lobby = await GetLobbyWithPlayersAsync(lobbyCode.NormalizeCode(), ct);

        if (lobby.Status != LobbyStatus.Waiting)
        {
            _logger.LogWarning("Leave rejected — lobby {LobbyCode} is not waiting (Status={Status})", lobby.Code, lobby.Status);
            throw new BadRequestException("Players can leave only while lobby is waiting.");
        }

        var membership = lobby.LobbyPlayers
            .FirstOrDefault(x => x.PlayerId == request.PlayerId && x.LeftAt == null);

        if (membership is null)
        {
            _logger.LogWarning("Leave rejected — player {PlayerId} not found as active in lobby {LobbyCode}", request.PlayerId, lobby.Code);
            throw new BadRequestException("Active player membership not found in this lobby.");
        }

        var wasHost = lobby.HostPlayerId == request.PlayerId;

        if (wasHost)
        {
            foreach (var m in lobby.LobbyPlayers.Where(x => x.LeftAt == null))
                m.LeftAt = now;

            lobby.Status = LobbyStatus.Finished;
            _logger.LogInformation("Host {PlayerId} left lobby {LobbyCode} — lobby closed", request.PlayerId, lobby.Code);
        }
        else
        {
            membership.LeftAt = now;
            _logger.LogInformation("Player {PlayerId} left lobby {LobbyCode}", request.PlayerId, lobby.Code);
        }

        await _dbContext.SaveChangesAsync(ct);
        await _notifier.LobbyUpdated(lobby.Code);

        return new LeaveLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            PlayerId = request.PlayerId,
            WasHost = wasHost,
            LobbyStatus = lobby.Status.ToContract()
        };
    }

    private Task<LobbyEntity> GetLobbyWithPlayersAsync(string normalizedCode, CancellationToken ct)
        => _dbContext.Lobbies.GetLobbyWithPlayersAsync(normalizedCode, ct);

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = _codeGenerator.Generate();
            var exists = await _dbContext.Lobbies.AnyAsync(x => x.Code == code, ct);
            if (!exists) return code;
        }

        _logger.LogError("Failed to generate unique lobby code after 10 attempts");
        throw new BadRequestException("Could not generate a unique lobby code.");
    }
}
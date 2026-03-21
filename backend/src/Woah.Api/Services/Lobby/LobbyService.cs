using Microsoft.EntityFrameworkCore;
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

    public LobbyService(WoahDbContext dbContext, ILobbyCodeGenerator codeGenerator, IGameNotifier notifier)
    {
        _dbContext = dbContext;
        _codeGenerator = codeGenerator;
        _notifier = notifier;
    }

    public async Task<CreateLobbyResponse> CreateLobbyAsync(CreateLobbyRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var nick = request.HostNick.Trim();
        var code = await GenerateUniqueCodeAsync(ct);

        var host = new PlayerEntity { PlayerId = Guid.NewGuid(), Nick = nick, CreatedAt = now };

        var lobby = new LobbyEntity
        {
            LobbyId = Guid.NewGuid(),
            Code = code,
            Status = LobbyStatus.Waiting,
            CreatedAt = now,
            HostPlayerId = host.PlayerId,
            HostPlayer = host,
            MaxPlayers = request.MaxPlayers
        };

        var membership = new LobbyPlayerEntity
        {
            LobbyId = lobby.LobbyId,
            PlayerId = host.PlayerId,
            Lobby = lobby,
            Player = host,
            Nick = nick,
            JoinedAt = now
        };

        var playlist = new PlaylistEntity
        {
            PlaylistId = Guid.NewGuid(),
            OwnerPlayerId = host.PlayerId,
            OwnerPlayer = host,
            Name = $"Lobby {code}",
            Market = "PL",
            CreatedAt = now
        };

        _dbContext.Players.Add(host);
        _dbContext.Lobbies.Add(lobby);
        _dbContext.LobbyPlayers.Add(membership);
        _dbContext.Playlists.Add(playlist);
        await _dbContext.SaveChangesAsync(ct);

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
        var now = DateTime.UtcNow;
        var nick = request.Nick.Trim();

        var lobby = await GetLobbyWithPlayersAsync(lobbyCode.NormalizeCode(), ct);

        if (lobby.Status != LobbyStatus.Waiting)
            throw new BadRequestException("Lobby is not accepting new players.");

        var active = lobby.ActivePlayers();

        if (active.Count >= lobby.MaxPlayers)
            throw new BadRequestException("Lobby is full.");

        if (active.Any(x => string.Equals(x.Nick, nick, StringComparison.OrdinalIgnoreCase)))
            throw new BadRequestException("Nick is already taken in this lobby.");

        var player = new PlayerEntity { PlayerId = Guid.NewGuid(), Nick = nick, CreatedAt = now };
        var membership = new LobbyPlayerEntity
        {
            LobbyId = lobby.LobbyId,
            PlayerId = player.PlayerId,
            Lobby = lobby,
            Player = player,
            Nick = nick,
            JoinedAt = now
        };

        _dbContext.Players.Add(player);
        _dbContext.LobbyPlayers.Add(membership);
        await _dbContext.SaveChangesAsync(ct);

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
            Status = lobby.Status,
            MaxPlayers = lobby.MaxPlayers,
            HostPlayerId = lobby.HostPlayerId,
            PlayerCount = active.Count,
            CurrentSessionId = sessionId,
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
        var now = DateTime.UtcNow;
        var lobby = await GetLobbyWithPlayersAsync(lobbyCode.NormalizeCode(), ct);

        if (lobby.Status != LobbyStatus.Waiting)
            throw new BadRequestException("Players can leave only while lobby is waiting.");

        var membership = (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .FirstOrDefault(x => x.PlayerId == request.PlayerId && x.LeftAt == null)
            ?? throw new BadRequestException("Active player membership not found in this lobby.");

        var wasHost = lobby.HostPlayerId == request.PlayerId;

        if (wasHost)
        {
            foreach (var m in (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>()).Where(x => x.LeftAt == null))
                m.LeftAt = now;

            lobby.Status = LobbyStatus.Finished;
        }
        else
        {
            membership.LeftAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);

        await _notifier.LobbyUpdated(lobby.Code);

        return new LeaveLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            PlayerId = request.PlayerId,
            WasHost = wasHost,
            LobbyStatus = lobby.Status
        };
    }

    private async Task<LobbyEntity> GetLobbyWithPlayersAsync(string normalizedCode, CancellationToken ct) =>
        await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, ct)
        ?? throw new NotFoundException("Lobby not found.");

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = _codeGenerator.Generate();
            var exists = await _dbContext.Lobbies.AnyAsync(x => x.Code == code, ct);
            if (!exists) return code;
        }

        throw new BadRequestException("Could not generate a unique lobby code.");
    }
}
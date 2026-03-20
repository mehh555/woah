using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Lobbies;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services;

public class LobbyService : ILobbyService
{
    private readonly WoahDbContext _dbContext;
    private readonly ILobbyCodeGenerator _lobbyCodeGenerator;

    public LobbyService(
        WoahDbContext dbContext,
        ILobbyCodeGenerator lobbyCodeGenerator)
    {
        _dbContext = dbContext;
        _lobbyCodeGenerator = lobbyCodeGenerator;
    }

    public async Task<CreateLobbyResponse> CreateLobbyAsync(
        CreateLobbyRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedNick = request.HostNick.Trim();
        var lobbyCode = await GenerateUniqueLobbyCodeAsync(cancellationToken);

        var hostPlayer = new PlayerEntity
        {
            PlayerId = Guid.NewGuid(),
            Nick = normalizedNick,
            CreatedAt = now
        };

        var lobby = new LobbyEntity
        {
            LobbyId = Guid.NewGuid(),
            Code = lobbyCode,
            Status = "Waiting",
            CreatedAt = now,
            HostPlayerId = hostPlayer.PlayerId,
            HostPlayer = hostPlayer,
            MaxPlayers = request.MaxPlayers
        };

        var lobbyPlayer = new LobbyPlayerEntity
        {
            LobbyId = lobby.LobbyId,
            PlayerId = hostPlayer.PlayerId,
            Lobby = lobby,
            Player = hostPlayer,
            Nick = normalizedNick,
            JoinedAt = now
        };

        var playlist = new PlaylistEntity
        {
            PlaylistId = Guid.NewGuid(),
            OwnerPlayerId = hostPlayer.PlayerId,
            OwnerPlayer = hostPlayer,
            Name = $"Lobby {lobbyCode}",
            Market = "PL",
            CreatedAt = now
        };

        _dbContext.Players.Add(hostPlayer);
        _dbContext.Lobbies.Add(lobby);
        _dbContext.LobbyPlayers.Add(lobbyPlayer);
        _dbContext.Playlists.Add(playlist);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            HostPlayerId = hostPlayer.PlayerId,
            PlaylistId = playlist.PlaylistId
        };
    }

    public async Task<JoinLobbyResponse> JoinLobbyAsync(
        string lobbyCode,
        JoinLobbyRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);
        var normalizedNick = request.Nick.Trim();
        var now = DateTime.UtcNow;

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedLobbyCode, cancellationToken);

        if (lobby is null)
        {
            throw new InvalidOperationException("Lobby not found.");
        }

        if (lobby.Status != "Waiting")
        {
            throw new InvalidOperationException("Lobby is not accepting new players.");
        }

        var activePlayers = GetActivePlayers(lobby);

        if (activePlayers.Count >= lobby.MaxPlayers)
        {
            throw new InvalidOperationException("Lobby is full.");
        }

        var nickAlreadyTaken = activePlayers.Any(x =>
            string.Equals(x.Nick, normalizedNick, StringComparison.OrdinalIgnoreCase));

        if (nickAlreadyTaken)
        {
            throw new InvalidOperationException("Nick is already taken in this lobby.");
        }

        var player = new PlayerEntity
        {
            PlayerId = Guid.NewGuid(),
            Nick = normalizedNick,
            CreatedAt = now
        };

        var lobbyPlayer = new LobbyPlayerEntity
        {
            LobbyId = lobby.LobbyId,
            PlayerId = player.PlayerId,
            Lobby = lobby,
            Player = player,
            Nick = normalizedNick,
            JoinedAt = now
        };

        _dbContext.Players.Add(player);
        _dbContext.LobbyPlayers.Add(lobbyPlayer);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new JoinLobbyResponse
        {
            PlayerId = player.PlayerId,
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            Nick = normalizedNick
        };
    }

    public async Task<GetLobbyResponse> GetLobbyAsync(
    string lobbyCode,
    CancellationToken cancellationToken = default)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedLobbyCode, cancellationToken);

        if (lobby is null)
        {
            throw new InvalidOperationException("Lobby not found.");
        }

        var activePlayers = GetActivePlayers(lobby);

        var currentSessionId = await _dbContext.GameSessions
            .Where(x => x.LobbyId == lobby.LobbyId && x.EndedAt == null)
            .Select(x => (Guid?)x.SessionId)
            .FirstOrDefaultAsync(cancellationToken);

        return new GetLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            Code = lobby.Code,
            Status = lobby.Status,
            MaxPlayers = lobby.MaxPlayers,
            HostPlayerId = lobby.HostPlayerId,
            PlayerCount = activePlayers.Count,
            CurrentSessionId = currentSessionId,
            Players = activePlayers.Select(x => new LobbyPlayerResponse
            {
                PlayerId = x.PlayerId,
                Nick = x.Nick,
                JoinedAt = x.JoinedAt,
                IsHost = x.PlayerId == lobby.HostPlayerId
            }).ToList()
        };
    }

    public async Task<LeaveLobbyResponse> LeaveLobbyAsync(
    string lobbyCode,
    LeaveLobbyRequest request,
    CancellationToken cancellationToken = default)
    {
        var normalizedLobbyCode = NormalizeLobbyCode(lobbyCode);
        var now = DateTime.UtcNow;

        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedLobbyCode, cancellationToken);

        if (lobby is null)
        {
            throw new InvalidOperationException("Lobby not found.");
        }

        if (lobby.Status != "Waiting")
        {
            throw new InvalidOperationException("Players can leave only while lobby is waiting.");
        }

        var membership = (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .FirstOrDefault(x => x.PlayerId == request.PlayerId && x.LeftAt == null);

        if (membership is null)
        {
            throw new InvalidOperationException("Active player membership not found in this lobby.");
        }

        var wasHost = lobby.HostPlayerId == request.PlayerId;

        if (wasHost)
        {
            foreach (var activeMembership in (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
                         .Where(x => x.LeftAt == null))
            {
                activeMembership.LeftAt = now;
            }

            lobby.Status = "Finished";
        }
        else
        {
            membership.LeftAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LeaveLobbyResponse
        {
            LobbyId = lobby.LobbyId,
            LobbyCode = lobby.Code,
            PlayerId = request.PlayerId,
            WasHost = wasHost,
            NewHostPlayerId = null,
            LobbyStatus = lobby.Status
        };
    }

    private async Task<string> GenerateUniqueLobbyCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = _lobbyCodeGenerator.Generate();

            var exists = await _dbContext.Lobbies
                .AnyAsync(x => x.Code == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not generate a unique lobby code.");
    }

    private static string NormalizeLobbyCode(string lobbyCode)
    {
        return lobbyCode.Trim().ToUpperInvariant();
    }

    private static List<LobbyPlayerEntity> GetActivePlayers(LobbyEntity lobby)
    {
        return (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .Where(x => x.LeftAt == null)
            .OrderBy(x => x.JoinedAt)
            .ToList();
    }
}
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
        var normalizedLobbyCode = lobbyCode.Trim().ToUpperInvariant();
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

        var activePlayers = (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .Where(x => x.LeftAt == null)
            .ToList();

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
}
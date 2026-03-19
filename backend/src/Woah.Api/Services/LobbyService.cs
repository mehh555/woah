using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Lobbies;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Infrastructure.Persistence;

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
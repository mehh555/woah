using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services;

public class SessionService : ISessionService
{
    private readonly WoahDbContext _dbContext;

    public SessionService(WoahDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StartSessionResponse> StartSessionAsync(
        string lobbyCode,
        StartSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedLobbyCode = lobbyCode.Trim().ToUpperInvariant();
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
            throw new InvalidOperationException("Only waiting lobbies can start a session.");
        }

        if (lobby.HostPlayerId != request.HostPlayerId)
        {
            throw new InvalidOperationException("Only the host can start the session.");
        }

        var activePlayers = (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .Where(x => x.LeftAt == null)
            .ToList();

        if (!activePlayers.Any(x => x.PlayerId == request.HostPlayerId))
        {
            throw new InvalidOperationException("Host is not active in this lobby.");
        }

        var existingActiveSession = await _dbContext.GameSessions
            .AnyAsync(x => x.LobbyId == lobby.LobbyId && x.EndedAt == null, cancellationToken);

        if (existingActiveSession)
        {
            throw new InvalidOperationException("An active session already exists for this lobby.");
        }

        var playlist = await _dbContext.Playlists
            .FirstOrDefaultAsync(
                x => x.PlaylistId == request.PlaylistId && x.OwnerPlayerId == request.HostPlayerId,
                cancellationToken);

        if (playlist is null)
        {
            throw new InvalidOperationException("Playlist not found for this host.");
        }

        var settingsJson = JsonSerializer.Serialize(new
        {
            roundDurationSeconds = request.RoundDurationSeconds
        });

        var session = new GameSessionEntity
        {
            SessionId = Guid.NewGuid(),
            LobbyId = lobby.LobbyId,
            PlaylistId = playlist.PlaylistId,
            StartedAt = now,
            EndedAt = null,
            SettingsJson = settingsJson,
            Lobby = lobby,
            Playlist = playlist
        };

        lobby.Status = "InGame";

        _dbContext.GameSessions.Add(session);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StartSessionResponse
        {
            SessionId = session.SessionId,
            LobbyId = lobby.LobbyId,
            PlaylistId = playlist.PlaylistId,
            HostPlayerId = request.HostPlayerId,
            StartedAt = session.StartedAt,
            LobbyStatus = lobby.Status,
            SettingsJson = session.SettingsJson
        };
    }
}
using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Integrations.Itunes;
using Woah.Api.Services.Notifications;

namespace Woah.Api.Services.Playlist;

public class LobbyPlaylistService : ILobbyPlaylistService
{
    private const int MaxPlaylistTracks = 20;

    private readonly WoahDbContext _dbContext;
    private readonly ItunesApiClient _itunesClient;
    private readonly ILobbyPlaylistStore _store;
    private readonly IGameNotifier _notifier;
    private readonly ILogger<LobbyPlaylistService> _logger;

    public LobbyPlaylistService(WoahDbContext dbContext, ItunesApiClient itunesClient, ILobbyPlaylistStore store, IGameNotifier notifier, ILogger<LobbyPlaylistService> logger)
    {
        _dbContext = dbContext;
        _itunesClient = itunesClient;
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<List<ItunesTrackSearchResultResponse>> SearchTracksAsync(string term, CancellationToken ct = default)
    {
        var results = await _itunesClient.SearchSongsAsync(term, ct);
        return results.Select(LobbyTrackMapper.ToSearchResult).ToList();
    }

    public async Task<GetLobbyPlaylistResponse> GetLobbyPlaylistAsync(string lobbyCode, CancellationToken ct = default)
    {
        var normalized = lobbyCode.NormalizeCode();
        await EnsureLobbyExistsAsync(normalized, ct);

        var tracks = _store.GetTracks(normalized);

        return new GetLobbyPlaylistResponse
        {
            LobbyCode = normalized,
            TrackCount = tracks.Count,
            Tracks = tracks.Select(LobbyTrackMapper.ToResponse).ToList()
        };
    }

    public async Task<GetLobbyPlaylistResponse> AddTrackAsync(string lobbyCode, AddLobbyTrackRequest request, CancellationToken ct = default)
    {
        var lobby = await GetLobbyForHostAsync(lobbyCode, request.HostPlayerId, ct);

        if (_store.GetTracks(lobby.Code).Count >= MaxPlaylistTracks)
        {
            _logger.LogWarning("Add track rejected — playlist limit reached for lobby {LobbyCode} ({Max} tracks)", lobby.Code, MaxPlaylistTracks);
            throw new BadRequestException($"Playlist cannot exceed {MaxPlaylistTracks} tracks.");
        }

        var track = await _itunesClient.LookupSongAsync(request.TrackId, ct);

        if (track is null)
        {
            _logger.LogWarning("Add track rejected — trackId={TrackId} not found in iTunes for lobby {LobbyCode}", request.TrackId, lobby.Code);
            throw new BadRequestException("Track not found in iTunes or preview is unavailable.");
        }

        if (!_store.TryAddTrack(lobby.Code, LobbyTrackMapper.ToDraft(track)))
        {
            _logger.LogWarning("Add track rejected — trackId={TrackId} already exists in lobby {LobbyCode} playlist", request.TrackId, lobby.Code);
            throw new BadRequestException("Track already exists in the lobby playlist.");
        }

        _logger.LogInformation("Track {TrackId} added to lobby {LobbyCode} playlist", request.TrackId, lobby.Code);

        await _notifier.LobbyUpdated(lobby.Code);

        return await GetLobbyPlaylistAsync(lobby.Code, ct);
    }

    public async Task<GetLobbyPlaylistResponse> RemoveTrackAsync(string lobbyCode, long trackId, RemoveLobbyTrackRequest request, CancellationToken ct = default)
    {
        var lobby = await GetLobbyForHostAsync(lobbyCode, request.HostPlayerId, ct);

        if (!_store.RemoveTrack(lobby.Code, trackId))
        {
            _logger.LogWarning("Remove track rejected — trackId={TrackId} not found in lobby {LobbyCode} playlist", trackId, lobby.Code);
            throw new BadRequestException("Track not found in the lobby playlist.");
        }

        _logger.LogInformation("Track {TrackId} removed from lobby {LobbyCode} playlist", trackId, lobby.Code);

        await _notifier.LobbyUpdated(lobby.Code);

        return await GetLobbyPlaylistAsync(lobby.Code, ct);
    }

    public void ClearLobbyPlaylist(string lobbyCode)
        => _store.Clear(lobbyCode.NormalizeCode());

    private async Task EnsureLobbyExistsAsync(string normalizedCode, CancellationToken ct)
    {
        var exists = await _dbContext.Lobbies.AnyAsync(x => x.Code == normalizedCode, ct);
        if (!exists)
            throw new NotFoundException("Lobby not found.");
    }

    private async Task<LobbyEntity> GetLobbyForHostAsync(string lobbyCode, Guid hostPlayerId, CancellationToken ct)
    {
        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == lobbyCode.NormalizeCode(), ct)
            ?? throw new NotFoundException("Lobby not found.");

        if (lobby.Status != LobbyStatus.Waiting)
        {
            _logger.LogWarning("Playlist action rejected — lobby {LobbyCode} is not waiting (Status={Status})", lobby.Code, lobby.Status);
            throw new BadRequestException("Tracks can be modified only while lobby is waiting.");
        }

        if (lobby.HostPlayerId != hostPlayerId)
        {
            _logger.LogWarning("Playlist action rejected — player {PlayerId} is not host of lobby {LobbyCode}", hostPlayerId, lobby.Code);
            throw new ForbiddenException("Only the host can modify the lobby playlist.");
        }

        if (!lobby.LobbyPlayers.Any(x => x.PlayerId == hostPlayerId && x.LeftAt == null))
        {
            _logger.LogWarning("Playlist action rejected — host {PlayerId} is not active in lobby {LobbyCode}", hostPlayerId, lobby.Code);
            throw new BadRequestException("Host is not active in this lobby.");
        }

        return lobby;
    }
}
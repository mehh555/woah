using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.InMemory;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Integrations.Itunes;

namespace Woah.Api.Services;

public class LobbyPlaylistService : ILobbyPlaylistService
{
    private readonly WoahDbContext _dbContext;
    private readonly ItunesApiClient _itunesApiClient;
    private readonly ILobbyPlaylistStore _store;

    public LobbyPlaylistService(
        WoahDbContext dbContext,
        ItunesApiClient itunesApiClient,
        ILobbyPlaylistStore store)
    {
        _dbContext = dbContext;
        _itunesApiClient = itunesApiClient;
        _store = store;
    }

    public async Task<List<ItunesTrackSearchResultResponse>> SearchTracksAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        var results = await _itunesApiClient.SearchSongsAsync(term, cancellationToken);

        return results.Select(x => new ItunesTrackSearchResultResponse
        {
            TrackId = x.TrackId,
            Title = x.TrackName!,
            Artist = x.ArtistName!,
            PreviewUrl = x.PreviewUrl!,
            ArtworkUrl = x.ArtworkUrl100,
            DurationMs = x.TrackTimeMillis,
            CollectionName = x.CollectionName
        }).ToList();
    }

    public async Task<GetLobbyPlaylistResponse> GetLobbyPlaylistAsync(
        string lobbyCode,
        CancellationToken cancellationToken = default)
    {
        await EnsureLobbyExistsAsync(lobbyCode, cancellationToken);

        var tracks = _store.GetTracks(lobbyCode);

        return new GetLobbyPlaylistResponse
        {
            LobbyCode = lobbyCode.NormalizeCode(),
            TrackCount = tracks.Count,
            Tracks = tracks.Select(MapTrack).ToList()
        };
    }

    public async Task<GetLobbyPlaylistResponse> AddTrackAsync(
        string lobbyCode,
        AddLobbyTrackRequest request,
        CancellationToken cancellationToken = default)
    {
        var lobby = await GetLobbyForHostActionAsync(lobbyCode, request.HostPlayerId, cancellationToken);

        var currentTracks = _store.GetTracks(lobby.Code);
        if (currentTracks.Count >= InMemoryLobbyPlaylistStore.MaxTracks)
            throw new InvalidOperationException($"Playlist cannot exceed {InMemoryLobbyPlaylistStore.MaxTracks} tracks.");

        var track = await _itunesApiClient.LookupSongAsync(request.TrackId, cancellationToken);

        if (track is null)
            throw new InvalidOperationException("Track not found in iTunes or preview is unavailable.");

        var added = _store.TryAddTrack(lobby.Code, new LobbyDraftTrack
        {
            TrackId = track.TrackId,
            Title = track.TrackName!,
            Artist = track.ArtistName!,
            PreviewUrl = track.PreviewUrl!,
            ArtworkUrl = track.ArtworkUrl100,
            DurationMs = track.TrackTimeMillis,
            AddedAt = DateTime.UtcNow
        });

        if (!added)
            throw new InvalidOperationException("Track already exists in the lobby playlist.");

        return await GetLobbyPlaylistAsync(lobby.Code, cancellationToken);
    }

    public async Task<GetLobbyPlaylistResponse> RemoveTrackAsync(
        string lobbyCode,
        long trackId,
        RemoveLobbyTrackRequest request,
        CancellationToken cancellationToken = default)
    {
        var lobby = await GetLobbyForHostActionAsync(lobbyCode, request.HostPlayerId, cancellationToken);

        if (!_store.RemoveTrack(lobby.Code, trackId))
            throw new InvalidOperationException("Track not found in the lobby playlist.");

        return await GetLobbyPlaylistAsync(lobby.Code, cancellationToken);
    }

    public void ClearLobbyPlaylist(string lobbyCode)
        => _store.Clear(lobbyCode);

    private async Task EnsureLobbyExistsAsync(string lobbyCode, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Lobbies
            .AnyAsync(x => x.Code == lobbyCode.NormalizeCode(), cancellationToken);

        if (!exists)
            throw new NotFoundException("Lobby not found.");
    }

    private async Task<LobbyEntity> GetLobbyForHostActionAsync(
        string lobbyCode,
        Guid hostPlayerId,
        CancellationToken cancellationToken)
    {
        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == lobbyCode.NormalizeCode(), cancellationToken);

        if (lobby is null)
            throw new NotFoundException("Lobby not found.");

        if (lobby.Status != LobbyStatus.Waiting)
            throw new InvalidOperationException("Tracks can be modified only while lobby is waiting.");

        if (lobby.HostPlayerId != hostPlayerId)
            throw new ForbiddenException("Only the host can modify the lobby playlist.");

        var hostIsActive = (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .Any(x => x.PlayerId == hostPlayerId && x.LeftAt == null);

        if (!hostIsActive)
            throw new InvalidOperationException("Host is not active in this lobby.");

        return lobby;
    }

    private static LobbyPlaylistTrackResponse MapTrack(LobbyDraftTrack track) =>
        new()
        {
            TrackId = track.TrackId,
            Title = track.Title,
            Artist = track.Artist,
            PreviewUrl = track.PreviewUrl,
            ArtworkUrl = track.ArtworkUrl,
            DurationMs = track.DurationMs,
            AddedAt = track.AddedAt
        };
}
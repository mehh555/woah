using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.InMemory;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Integrations.Itunes;

namespace Woah.Api.Services.Playlist;

public class LobbyPlaylistService : ILobbyPlaylistService
{
    private readonly WoahDbContext _dbContext;
    private readonly ItunesApiClient _itunesClient;
    private readonly ILobbyPlaylistStore _store;

    public LobbyPlaylistService(WoahDbContext dbContext, ItunesApiClient itunesClient, ILobbyPlaylistStore store)
    {
        _dbContext = dbContext;
        _itunesClient = itunesClient;
        _store = store;
    }

    public async Task<List<ItunesTrackSearchResultResponse>> SearchTracksAsync(string term, CancellationToken ct = default)
    {
        var results = await _itunesClient.SearchSongsAsync(term, ct);
        return results.Select(LobbyTrackMapper.ToSearchResult).ToList();
    }

    public async Task<GetLobbyPlaylistResponse> GetLobbyPlaylistAsync(string lobbyCode, CancellationToken ct = default)
    {
        await EnsureLobbyExistsAsync(lobbyCode, ct);

        var tracks = _store.GetTracks(lobbyCode);

        return new GetLobbyPlaylistResponse
        {
            LobbyCode = lobbyCode.NormalizeCode(),
            TrackCount = tracks.Count,
            Tracks = tracks.Select(LobbyTrackMapper.ToResponse).ToList()
        };
    }

    public async Task<GetLobbyPlaylistResponse> AddTrackAsync(string lobbyCode, AddLobbyTrackRequest request, CancellationToken ct = default)
    {
        var lobby = await GetLobbyForHostAsync(lobbyCode, request.HostPlayerId, ct);

        if (_store.GetTracks(lobby.Code).Count >= InMemoryLobbyPlaylistStore.MaxTracks)
            throw new InvalidOperationException($"Playlist cannot exceed {InMemoryLobbyPlaylistStore.MaxTracks} tracks.");

        var track = await _itunesClient.LookupSongAsync(request.TrackId, ct)
            ?? throw new InvalidOperationException("Track not found in iTunes or preview is unavailable.");

        if (!_store.TryAddTrack(lobby.Code, LobbyTrackMapper.ToDraft(track)))
            throw new InvalidOperationException("Track already exists in the lobby playlist.");

        return await GetLobbyPlaylistAsync(lobby.Code, ct);
    }

    public async Task<GetLobbyPlaylistResponse> RemoveTrackAsync(string lobbyCode, long trackId, RemoveLobbyTrackRequest request, CancellationToken ct = default)
    {
        var lobby = await GetLobbyForHostAsync(lobbyCode, request.HostPlayerId, ct);

        if (!_store.RemoveTrack(lobby.Code, trackId))
            throw new InvalidOperationException("Track not found in the lobby playlist.");

        return await GetLobbyPlaylistAsync(lobby.Code, ct);
    }

    public void ClearLobbyPlaylist(string lobbyCode)
        => _store.Clear(lobbyCode);

    private async Task EnsureLobbyExistsAsync(string lobbyCode, CancellationToken ct)
    {
        var exists = await _dbContext.Lobbies.AnyAsync(x => x.Code == lobbyCode.NormalizeCode(), ct);
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
            throw new InvalidOperationException("Tracks can be modified only while lobby is waiting.");

        if (lobby.HostPlayerId != hostPlayerId)
            throw new ForbiddenException("Only the host can modify the lobby playlist.");

        if (!(lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>()).Any(x => x.PlayerId == hostPlayerId && x.LeftAt == null))
            throw new InvalidOperationException("Host is not active in this lobby.");

        return lobby;
    }
}
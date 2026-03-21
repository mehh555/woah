using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Domain;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Integrations.Itunes;

namespace Woah.Api.Services.Playlist;

public class LobbyPlaylistService : ILobbyPlaylistService
{
    private const int MaxPlaylistTracks = 20;

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
            throw new BadRequestException($"Playlist cannot exceed {MaxPlaylistTracks} tracks.");

        var track = await _itunesClient.LookupSongAsync(request.TrackId, ct)
            ?? throw new BadRequestException("Track not found in iTunes or preview is unavailable.");

        if (!_store.TryAddTrack(lobby.Code, LobbyTrackMapper.ToDraft(track)))
            throw new BadRequestException("Track already exists in the lobby playlist.");

        return await GetLobbyPlaylistAsync(lobby.Code, ct);
    }

    public async Task<GetLobbyPlaylistResponse> RemoveTrackAsync(string lobbyCode, long trackId, RemoveLobbyTrackRequest request, CancellationToken ct = def
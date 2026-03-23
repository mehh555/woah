using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    private readonly WoahDbContext _dbContext;
    private readonly ItunesApiClient _itunesClient;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LobbyPlaylistService> _logger;

    public LobbyPlaylistService(
        WoahDbContext dbContext,
        ItunesApiClient itunesClient,
        IGameNotifier notifier,
        TimeProvider timeProvider,
        ILogger<LobbyPlaylistService> logger)
    {
        _dbContext = dbContext;
        _itunesClient = itunesClient;
        _notifier = notifier;
        _timeProvider = timeProvider;
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

        var lobby = await _dbContext.Lobbies
            .FirstOrDefaultAsync(x => x.Code == normalized, ct)
            ?? throw new NotFoundException("Lobby not found.");

        var tracks = await _dbContext.PlaylistTracks
            .Where(x => x.PlaylistId == lobby.ActivePlaylistId)
            .OrderBy(x => x.AddedAt)
            .ToListAsync(ct);

        return new GetLobbyPlaylistResponse
        {
            LobbyCode = normalized,
            TrackCount = tracks.Count,
            Tracks = tracks.Select(LobbyTrackMapper.ToResponse).ToList()
        };
    }

    public async Task<GetLobbyPlaylistResponse> AddTrackAsync(string lobbyCode, AddLobbyTrackRequest request, CancellationToken ct = default)
    {
        var (lobby, playlist) = await GetLobbyForActivePlayerAsync(lobbyCode, request.PlayerId, ct);

        var playerTrackCount = await _dbContext.PlaylistTracks
            .CountAsync(x => x.PlaylistId == playlist.PlaylistId && x.AddedByPlayerId == request.PlayerId, ct);

        if (playerTrackCount >= ILobbyPlaylistService.MaxTracksPerPlayer)
        {
            _logger.LogWarning("Add track rejected — player {PlayerId} reached limit in lobby {LobbyCode} ({Max} tracks)",
                request.PlayerId, lobby.Code, ILobbyPlaylistService.MaxTracksPerPlayer);
            throw new BadRequestException($"You can add at most {ILobbyPlaylistService.MaxTracksPerPlayer} tracks.");
        }

        var itunesTrack = await _itunesClient.LookupSongAsync(request.TrackId, ct);

        if (itunesTrack is null)
        {
            _logger.LogWarning("Add track rejected — trackId={TrackId} not found in iTunes for lobby {LobbyCode}",
                request.TrackId, lobby.Code);
            throw new BadRequestException("Track not found in iTunes or preview is unavailable.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = LobbyTrackMapper.ToEntity(itunesTrack, playlist.PlaylistId, request.PlayerId, now);
        _dbContext.PlaylistTracks.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning("Add track rejected — trackId={TrackId} already exists in lobby {LobbyCode} playlist",
                request.TrackId, lobby.Code);
            throw new BadRequestException("Track already exists in the lobby playlist.");
        }

        _logger.LogInformation("Track {TrackId} added by player {PlayerId} to lobby {LobbyCode} playlist",
            request.TrackId, request.PlayerId, lobby.Code);
        await _notifier.LobbyUpdated(lobby.Code);

        return await GetLobbyPlaylistAsync(lobby.Code, ct);
    }

    public async Task<GetLobbyPlaylistResponse> RemoveTrackAsync(string lobbyCode, long trackId, RemoveLobbyTrackRequest request, CancellationToken ct = default)
    {
        var (lobby, playlist) = await GetLobbyForActivePlayerAsync(lobbyCode, request.PlayerId, ct);

        var track = await _dbContext.PlaylistTracks
            .FirstOrDefaultAsync(x => x.PlaylistId == playlist.PlaylistId && x.ItunesTrackId == trackId, ct);

        if (track is null)
        {
            _logger.LogWarning("Remove track rejected — trackId={TrackId} not found in lobby {LobbyCode} playlist",
                trackId, lobby.Code);
            throw new BadRequestException("Track not found in the lobby playlist.");
        }

        if (track.AddedByPlayerId != request.PlayerId)
        {
            _logger.LogWarning("Remove track rejected — player {PlayerId} did not add trackId={TrackId} in lobby {LobbyCode}",
                request.PlayerId, trackId, lobby.Code);
            throw new ForbiddenException("You can only remove tracks you added.");
        }

        _dbContext.PlaylistTracks.Remove(track);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Track {TrackId} removed by player {PlayerId} from lobby {LobbyCode} playlist",
            trackId, request.PlayerId, lobby.Code);
        await _notifier.LobbyUpdated(lobby.Code);

        return await GetLobbyPlaylistAsync(lobby.Code, ct);
    }

    private async Task<(LobbyEntity Lobby, PlaylistEntity Playlist)> GetLobbyForActivePlayerAsync(
        string lobbyCode, Guid playerId, CancellationToken ct)
    {
        var lobby = await _dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == lobbyCode.NormalizeCode(), ct)
            ?? throw new NotFoundException("Lobby not found.");

        if (lobby.Status != LobbyStatus.Waiting)
        {
            _logger.LogWarning("Playlist action rejected — lobby {LobbyCode} is not waiting (Status={Status})",
                lobby.Code, lobby.Status);
            throw new BadRequestException("Tracks can be modified only while lobby is waiting.");
        }

        if (!lobby.LobbyPlayers.Any(x => x.PlayerId == playerId && x.LeftAt == null))
        {
            _logger.LogWarning("Playlist action rejected — player {PlayerId} is not active in lobby {LobbyCode}",
                playerId, lobby.Code);
            throw new BadRequestException("Player is not active in this lobby.");
        }

        var playlist = await _dbContext.Playlists
            .FirstOrDefaultAsync(x => x.PlaylistId == lobby.ActivePlaylistId, ct)
            ?? throw new NotFoundException("Active playlist not found.");

        return (lobby, playlist);
    }
}
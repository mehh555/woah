using Woah.Api.Contracts.Playlists;

namespace Woah.Api.Services;

public interface ILobbyPlaylistService
{
    Task<List<ItunesTrackSearchResultResponse>> SearchTracksAsync(string term, CancellationToken cancellationToken = default);
    Task<GetLobbyPlaylistResponse> GetLobbyPlaylistAsync(string lobbyCode, CancellationToken cancellationToken = default);
    Task<GetLobbyPlaylistResponse> AddTrackAsync(string lobbyCode, AddLobbyTrackRequest request, CancellationToken cancellationToken = default);
    Task<GetLobbyPlaylistResponse> RemoveTrackAsync(string lobbyCode, long trackId, RemoveLobbyTrackRequest request, CancellationToken cancellationToken = default);
    void ClearLobbyPlaylist(string lobbyCode);
}
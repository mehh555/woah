namespace Woah.Api.Services.Playlist;

public interface ILobbyPlaylistStore
{
    const int MaxTracks = 30;
    IReadOnlyList<LobbyDraftTrack> GetTracks(string lobbyCode);
    bool TryAddTrack(string lobbyCode, LobbyDraftTrack track);
    bool RemoveTrack(string lobbyCode, long trackId);
    void Clear(string lobbyCode);
}
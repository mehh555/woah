namespace Woah.Api.Services;

public interface ILobbyPlaylistStore
{
    IReadOnlyList<LobbyDraftTrack> GetTracks(string lobbyCode);
    bool TryAddTrack(string lobbyCode, LobbyDraftTrack track);
    bool RemoveTrack(string lobbyCode, long trackId);
    void Clear(string lobbyCode);
}
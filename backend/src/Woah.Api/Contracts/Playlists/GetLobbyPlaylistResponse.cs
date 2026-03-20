using System.Collections.Generic;

namespace Woah.Api.Contracts.Playlists;

public class GetLobbyPlaylistResponse
{
    public string LobbyCode { get; set; } = default!;
    public int TrackCount { get; set; }
    public List<LobbyPlaylistTrackResponse> Tracks { get; set; } = new();
}
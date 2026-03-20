using System;

namespace Woah.Api.Contracts.Playlists;

public class AddLobbyTrackRequest
{
    public Guid HostPlayerId { get; set; }
    public long TrackId { get; set; }
}
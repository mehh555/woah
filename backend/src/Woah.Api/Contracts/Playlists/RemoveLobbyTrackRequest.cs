using System;

namespace Woah.Api.Contracts.Playlists;

public class RemoveLobbyTrackRequest
{
    public Guid HostPlayerId { get; set; }
}
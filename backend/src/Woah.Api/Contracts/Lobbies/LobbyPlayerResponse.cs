using System;

namespace Woah.Api.Contracts.Lobbies;

public class LobbyPlayerResponse
{
    public Guid PlayerId { get; set; }
    public string Nick { get; set; } = default!;
    public bool IsHost { get; set; }
    public DateTime JoinedAt { get; set; }
}
using System;

namespace Woah.Api.Contracts.Lobbies;

public class LeaveLobbyResponse
{
    public Guid LobbyId { get; set; }
    public string LobbyCode { get; set; } = default!;
    public Guid PlayerId { get; set; }
    public bool WasHost { get; set; }
    public Guid? NewHostPlayerId { get; set; }
    public string LobbyStatus { get; set; } = default!;
}
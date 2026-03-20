using System;
using System.Collections.Generic;

namespace Woah.Api.Contracts.Lobbies;

public class GetLobbyResponse
{
    public Guid LobbyId { get; set; }
    public string Code { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int MaxPlayers { get; set; }
    public Guid HostPlayerId { get; set; }
    public int PlayerCount { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public List<LobbyPlayerResponse> Players { get; set; } = new();
}
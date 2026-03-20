using System;

namespace Woah.Api.Contracts.Lobbies;

public class JoinLobbyResponse
{
    public Guid PlayerId { get; set; }
    public Guid LobbyId { get; set; }
    public string LobbyCode { get; set; } = default!;
    public string Nick { get; set; } = default!;
}
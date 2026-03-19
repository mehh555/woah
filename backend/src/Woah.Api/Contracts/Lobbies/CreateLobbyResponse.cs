using System;

namespace Woah.Api.Contracts.Lobbies;

public class CreateLobbyResponse
{
    public Guid LobbyId { get; set; }
    public string LobbyCode { get; set; } = default!;
    public Guid HostPlayerId { get; set; }
    public Guid PlaylistId { get; set; }
}
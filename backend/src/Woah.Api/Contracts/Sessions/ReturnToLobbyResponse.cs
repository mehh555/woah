namespace Woah.Api.Contracts.Sessions;

public class ReturnToLobbyResponse
{
    public Guid LobbyId { get; set; }
    public string LobbyCode { get; set; } = default!;
    public Guid PlaylistId { get; set; }
    public string LobbyStatus { get; set; } = default!;
}
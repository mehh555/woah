namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class Lobby
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Status { get; set; } = "waiting";
    public DateTimeOffset CreatedAt { get; set; }

    public Guid HostPlayerId { get; set; }
    public short MaxPlayers { get; set; } = 10;

    public Player HostPlayer { get; set; } = null!;
    public ICollection<LobbyPlayer> Players { get; set; } = new List<LobbyPlayer>();
}
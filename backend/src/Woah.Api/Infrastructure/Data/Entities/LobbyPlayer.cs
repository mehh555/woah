namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class LobbyPlayer
{
    public Guid LobbyId { get; set; }
    public Guid PlayerId { get; set; }

    public string Nick { get; set; } = null!;
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }

    public Lobby Lobby { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
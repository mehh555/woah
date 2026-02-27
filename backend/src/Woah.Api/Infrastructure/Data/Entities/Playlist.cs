namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class Playlist
{
    public Guid Id { get; set; }
    public Guid OwnerPlayerId { get; set; }
    public string Name { get; set; } = null!;
    public string Market { get; set; } = "PL";
    public DateTimeOffset CreatedAt { get; set; }

    public Player OwnerPlayer { get; set; } = null!;
    public ICollection<PlaylistTrack> Tracks { get; set; } = new List<PlaylistTrack>();
}
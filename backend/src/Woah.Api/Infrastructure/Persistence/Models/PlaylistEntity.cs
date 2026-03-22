namespace Woah.Api.Infrastructure.Persistence.Models;

public class PlaylistEntity
{
    public Guid PlaylistId { get; set; }
    public Guid OwnerPlayerId { get; set; }
    public PlayerEntity? OwnerPlayer { get; set; }

    public string Name { get; set; } = default!;
    public string Market { get; set; } = "PL";
    public DateTime CreatedAt { get; set; }

    public ICollection<PlaylistTrackEntity> Tracks { get; set; } = new List<PlaylistTrackEntity>();
}
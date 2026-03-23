namespace Woah.Api.Infrastructure.Persistence.Models;

public class PlaylistTrackEntity
{
    public Guid PlaylistTrackId { get; set; }
    public Guid PlaylistId { get; set; }
    public PlaylistEntity? Playlist { get; set; }

    public Guid AddedByPlayerId { get; set; }
    public PlayerEntity? AddedByPlayer { get; set; }

    public long ItunesTrackId { get; set; }
    public string Title { get; set; } = default!;
    public string Artist { get; set; } = default!;
    public string PreviewUrl { get; set; } = default!;
    public string? ArtworkUrl { get; set; }
    public int? DurationMs { get; set; }
    public DateTime AddedAt { get; set; }
}
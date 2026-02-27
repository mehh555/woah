namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class PlaylistTrack
{
    public Guid PlaylistId { get; set; }
    public int ItemNo { get; set; }

    public string TrackJson { get; set; } = "{}";
    public string Title { get; set; } = null!;
    public string? PreviewUrl { get; set; }
    public string? SpotifyTrackId { get; set; }
    public string? SpotifyUrl { get; set; }

    public bool IsValid { get; set; } = true;
    public string? InvalidReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Playlist Playlist { get; set; } = null!;
}
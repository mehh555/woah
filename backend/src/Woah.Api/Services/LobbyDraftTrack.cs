namespace Woah.Api.Services;

public class LobbyDraftTrack
{
    public long TrackId { get; set; }
    public string Title { get; set; } = default!;
    public string Artist { get; set; } = default!;
    public string PreviewUrl { get; set; } = default!;
    public string? ArtworkUrl { get; set; }
    public int? DurationMs { get; set; }
    public DateTime AddedAt { get; set; }
}
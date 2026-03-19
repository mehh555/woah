namespace Woah.Api.Itunes;

public class ItunesSongResult
{
    public long TrackId { get; set; }
    public string TrackName { get; set; } = default!;
    public string ArtistName { get; set; } = default!;
    public string? CollectionName { get; set; }
    public string PreviewUrl { get; set; } = default!;
    public string? ArtworkUrl { get; set; }
    public int? TrackTimeMillis { get; set; }
}
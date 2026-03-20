using System.Text.Json.Serialization;

namespace Woah.Api.Integrations.Itunes;

public class ItunesTrackDto
{
    [JsonPropertyName("trackId")]
    public long TrackId { get; set; }

    [JsonPropertyName("trackName")]
    public string? TrackName { get; set; }

    [JsonPropertyName("artistName")]
    public string? ArtistName { get; set; }

    [JsonPropertyName("previewUrl")]
    public string? PreviewUrl { get; set; }

    [JsonPropertyName("artworkUrl100")]
    public string? ArtworkUrl100 { get; set; }

    [JsonPropertyName("trackTimeMillis")]
    public int? TrackTimeMillis { get; set; }

    [JsonPropertyName("collectionName")]
    public string? CollectionName { get; set; }
}
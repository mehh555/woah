using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Woah.Api.Itunes;

public class ItunesSearchResponse
{
    [JsonPropertyName("resultCount")]
    public int ResultCount { get; set; }

    [JsonPropertyName("results")]
    public List<ItunesSongDto> Results { get; set; } = new();
}

public class ItunesSongDto
{
    [JsonPropertyName("trackId")]
    public long TrackId { get; set; }

    [JsonPropertyName("trackName")]
    public string? TrackName { get; set; }

    [JsonPropertyName("artistName")]
    public string? ArtistName { get; set; }

    [JsonPropertyName("collectionName")]
    public string? CollectionName { get; set; }

    [JsonPropertyName("previewUrl")]
    public string? PreviewUrl { get; set; }

    [JsonPropertyName("artworkUrl100")]
    public string? ArtworkUrl100 { get; set; }

    [JsonPropertyName("trackTimeMillis")]
    public int? TrackTimeMillis { get; set; }
}
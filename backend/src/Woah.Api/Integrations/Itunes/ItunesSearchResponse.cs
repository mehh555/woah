using System.Text.Json.Serialization;

namespace Woah.Api.Integrations.Itunes;

public class ItunesSearchResponse
{
    [JsonPropertyName("resultCount")]
    public int ResultCount { get; set; }

    [JsonPropertyName("results")]
    public List<ItunesTrackDto> Results { get; set; } = new();
}
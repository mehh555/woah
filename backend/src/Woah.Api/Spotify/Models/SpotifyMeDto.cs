using System.Text.Json.Serialization;

namespace Woah.Api.Spotify.Models;

public sealed class SpotifyMeDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("product")]
    public string? Product { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }
}
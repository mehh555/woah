using System.Text.Json.Serialization;

namespace Woah.Api.Spotify.Models;

public sealed class SpotifyPlaylistItemDto
{
    [JsonPropertyName("item")]
    public SpotifyTrackDto? Item { get; init; }

    [JsonPropertyName("is_local")]
    public bool? IsLocal { get; init; }
}

public sealed class SpotifyTrackDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; init; }

    [JsonPropertyName("duration_ms")]
    public int? DurationMs { get; init; }

    [JsonPropertyName("explicit")]
    public bool? Explicit { get; init; }

    [JsonPropertyName("is_local")]
    public bool? IsLocal { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("artists")]
    public List<SpotifyArtistDto> Artists { get; init; } = [];
}

public sealed class SpotifyArtistDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
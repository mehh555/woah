using System.Text.Json.Serialization;

namespace Woah.Api.Spotify.Models;

public sealed class SpotifyPlaylistDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("public")]
    public bool? Public { get; init; }

    [JsonPropertyName("snapshot_id")]
    public string? SnapshotId { get; init; }

    [JsonPropertyName("owner")]
    public SpotifyPlaylistOwnerDto? Owner { get; init; }

    [JsonPropertyName("items")]
    public SpotifyPlaylistItemsDto? Items { get; init; }

    [JsonPropertyName("tracks")]
    public SpotifyPlaylistItemsDto? DeprecatedTracks { get; init; }
}

public sealed class SpotifyPlaylistOwnerDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }
}

public sealed class SpotifyPlaylistItemsDto
{
    [JsonPropertyName("href")]
    public string? Href { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }
}
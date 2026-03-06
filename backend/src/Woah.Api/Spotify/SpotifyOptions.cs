using System.ComponentModel.DataAnnotations;

namespace Woah.Api.Spotify;

public sealed class SpotifyOptions
{
    public const string SectionName = "Spotify";

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    [Required]
    public string RedirectUri { get; init; } = string.Empty;
}
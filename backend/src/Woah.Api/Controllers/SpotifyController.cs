using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Woah.Api.Spotify;

namespace Woah.Api.Controllers;

[ApiController]
[Route("spotify")]
public sealed class SpotifyController : ControllerBase
{
    private const string StateCookieName = "spotify_auth_state";

    private static readonly string[] DefaultScopes =
    [
        "user-read-private",
        "playlist-read-private"
    ];

    private readonly SpotifyAuthService _spotifyAuthService;
    private readonly SpotifyApiClient _spotifyApiClient;

    public SpotifyController(
        SpotifyAuthService spotifyAuthService,
        SpotifyApiClient spotifyApiClient)
    {
        _spotifyAuthService = spotifyAuthService;
        _spotifyApiClient = spotifyApiClient;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        Response.Cookies.Append(
            StateCookieName,
            state,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                MaxAge = TimeSpan.FromMinutes(10)
            });

        var authorizationUrl = _spotifyAuthService.BuildAuthorizationUrl(state, DefaultScopes);

        return Redirect(authorizationUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(CancellationToken cancellationToken)
    {
        var code = Request.Query["code"].ToString();
        var state = Request.Query["state"].ToString();
        var error = Request.Query["error"].ToString();

        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(new
            {
                message = "Spotify authorization failed.",
                error
            });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new
            {
                message = "Spotify callback did not contain an authorization code."
            });
        }

        if (!Request.Cookies.TryGetValue(StateCookieName, out var expectedState) ||
            string.IsNullOrWhiteSpace(expectedState))
        {
            return BadRequest(new
            {
                message = "Missing stored Spotify auth state."
            });
        }

        if (!string.Equals(expectedState, state, StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                message = "Spotify auth state mismatch."
            });
        }

        Response.Cookies.Delete(StateCookieName);

        var token = await _spotifyAuthService.ExchangeCodeForTokenAsync(code, cancellationToken);
        var profile = await _spotifyApiClient.GetCurrentUserProfileAsync(token.AccessToken, cancellationToken);

        return Ok(new
        {
            message = "Spotify authorization completed successfully.",
            accessToken = token.AccessToken,
            tokenType = token.TokenType,
            expiresIn = token.ExpiresIn,
            scope = token.Scope,
            profile
        });
    }

    [HttpGet("playlists")]
    public async Task<IActionResult> GetPlaylists(
        [FromQuery] string accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return BadRequest(new
            {
                message = "Query parameter 'accessToken' is required."
            });
        }

        var playlists = await _spotifyApiClient.GetCurrentUserPlaylistsAsync(accessToken, cancellationToken);

        return Ok(playlists.Select(playlist => new
        {
            playlist.Id,
            playlist.Name,
            playlist.Description,
            playlist.Public,
            playlist.SnapshotId,
            OwnerId = playlist.Owner?.Id,
            OwnerDisplayName = playlist.Owner?.DisplayName,
            ItemCount = playlist.Items?.Total
                        ?? playlist.DeprecatedTracks?.Total
                        ?? 0
        }));
    }

    [HttpGet("playlists/{playlistId}/items")]
    public async Task<IActionResult> GetPlaylistItems(
    [FromRoute] string playlistId,
    [FromQuery] string accessToken,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return BadRequest(new
            {
                message = "Query parameter 'accessToken' is required."
            });
        }

        if (string.IsNullOrWhiteSpace(playlistId))
        {
            return BadRequest(new
            {
                message = "Route parameter 'playlistId' is required."
            });
        }

        var items = await _spotifyApiClient.GetPlaylistItemsAsync(accessToken, playlistId, cancellationToken);

        return Ok(items
            .Where(x => x.Item is not null)
            .Where(x => string.Equals(x.Item!.Type, "track", StringComparison.OrdinalIgnoreCase))
            .Select(x => new
            {
                x.Item!.Id,
                x.Item.Name,
                x.Item.Uri,
                x.Item.PreviewUrl,
                x.Item.DurationMs,
                x.Item.Explicit,
                IsLocal = x.Item.IsLocal ?? x.IsLocal,
                Artists = x.Item.Artists
                    .Select(artist => artist.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
            }));
    }
}
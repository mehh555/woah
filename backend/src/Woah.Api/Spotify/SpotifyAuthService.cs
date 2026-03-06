using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Woah.Api.Spotify.Models;

namespace Woah.Api.Spotify;

public sealed class SpotifyAuthService
{
    private const string AuthorizationEndpoint = "https://accounts.spotify.com/authorize";
    private const string TokenEndpoint = "https://accounts.spotify.com/api/token";

    private readonly HttpClient _httpClient;
    private readonly SpotifyOptions _options;

    public SpotifyAuthService(HttpClient httpClient, IOptions<SpotifyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string BuildAuthorizationUrl(string state, IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("State cannot be empty.", nameof(state));
        }

        var normalizedScopes = scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var queryParams = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _options.RedirectUri,
            ["state"] = state,
            ["scope"] = string.Join(' ', normalizedScopes)
        };

        return QueryHelpers.AddQueryString(AuthorizationEndpoint, queryParams);
    }

    public async Task<SpotifyTokenDto> ExchangeCodeForTokenAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Authorization code cannot be empty.", nameof(code));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _options.RedirectUri
            })
        };

        var basicCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Spotify token exchange failed. StatusCode={(int)response.StatusCode}, Body={responseBody}");
        }

        var token = await response.Content.ReadFromJsonAsync<SpotifyTokenDto>(cancellationToken: cancellationToken);

        if (token is null)
        {
            throw new InvalidOperationException("Spotify returned an empty token response.");
        }

        return token;
    }
}
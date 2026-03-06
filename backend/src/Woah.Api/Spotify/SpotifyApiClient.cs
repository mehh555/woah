using System.Net.Http.Headers;
using System.Net.Http.Json;
using Woah.Api.Spotify.Models;

namespace Woah.Api.Spotify;

public sealed class SpotifyApiClient
{
    private readonly HttpClient _httpClient;

    public SpotifyApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SpotifyMeDto> GetCurrentUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access token cannot be empty.", nameof(accessToken));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Spotify profile request failed. StatusCode={(int)response.StatusCode}, Body={responseBody}");
        }

        var profile = await response.Content.ReadFromJsonAsync<SpotifyMeDto>(cancellationToken: cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException("Spotify returned an empty profile response.");
        }

        return profile;
    }

    public async Task<IReadOnlyList<SpotifyPlaylistDto>> GetCurrentUserPlaylistsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access token cannot be empty.", nameof(accessToken));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/playlists?limit=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Spotify playlists request failed. StatusCode={(int)response.StatusCode}, Body={responseBody}");
        }

        var paging = await response.Content.ReadFromJsonAsync<SpotifyPagingResponse<SpotifyPlaylistDto>>(
            cancellationToken: cancellationToken);

        if (paging is null)
        {
            throw new InvalidOperationException("Spotify returned an empty playlists response.");
        }

        return paging.Items;
    }

    public async Task<IReadOnlyList<SpotifyPlaylistItemDto>> GetPlaylistItemsAsync(
        string accessToken,
        string playlistId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access token cannot be empty.", nameof(accessToken));
        }

        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentException("PlaylistId cannot be empty.", nameof(playlistId));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.spotify.com/v1/playlists/{playlistId}/items?limit=50");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Spotify playlist items request failed. StatusCode={(int)response.StatusCode}, Body={responseBody}");
        }

        var paging = await response.Content.ReadFromJsonAsync<SpotifyPagingResponse<SpotifyPlaylistItemDto>>(
            cancellationToken: cancellationToken);

        if (paging is null)
        {
            throw new InvalidOperationException("Spotify returned an empty playlist items response.");
        }

        return paging.Items;
    }
}
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Woah.Api.Integrations.Itunes;

public class ItunesApiClient
{
    private readonly HttpClient _httpClient;

    public ItunesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ItunesTrackDto>> SearchSongsAsync(string term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<ItunesTrackDto>();
        }

        var url = QueryHelpers.AddQueryString("search", new Dictionary<string, string?>
        {
            ["term"] = term,
            ["country"] = "PL",
            ["media"] = "music",
            ["entity"] = "song",
            ["limit"] = "12"
        });

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ItunesSearchResponse>(cancellationToken: cancellationToken);

        return payload?.Results?
            .Where(x =>
                x.TrackId > 0 &&
                !string.IsNullOrWhiteSpace(x.TrackName) &&
                !string.IsNullOrWhiteSpace(x.ArtistName) &&
                !string.IsNullOrWhiteSpace(x.PreviewUrl))
            .ToList()
            ?? new List<ItunesTrackDto>();
    }

    public async Task<ItunesTrackDto?> LookupSongAsync(long trackId, CancellationToken cancellationToken = default)
    {
        var url = QueryHelpers.AddQueryString("lookup", new Dictionary<string, string?>
        {
            ["id"] = trackId.ToString(),
            ["country"] = "PL",
            ["entity"] = "song"
        });

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ItunesSearchResponse>(cancellationToken: cancellationToken);

        return payload?.Results?
            .FirstOrDefault(x =>
                x.TrackId > 0 &&
                !string.IsNullOrWhiteSpace(x.TrackName) &&
                !string.IsNullOrWhiteSpace(x.ArtistName) &&
                !string.IsNullOrWhiteSpace(x.PreviewUrl));
    }
}
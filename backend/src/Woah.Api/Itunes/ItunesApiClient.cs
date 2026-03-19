using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Woah.Api.Itunes;

public class ItunesApiClient
{
    private readonly HttpClient _httpClient;

    public ItunesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ItunesSongResult>> SearchSongsAsync(string term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<ItunesSongResult>();
        }

        var url = QueryHelpers.AddQueryString("search", new Dictionary<string, string?>
        {
            ["term"] = term,
            ["country"] = "PL",
            ["media"] = "music",
            ["entity"] = "song",
            ["limit"] = "10"
        });

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ItunesSearchResponse>(cancellationToken: cancellationToken);

        if (payload?.Results == null)
        {
            return new List<ItunesSongResult>();
        }

        return payload.Results
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.TrackName) &&
                !string.IsNullOrWhiteSpace(x.ArtistName) &&
                !string.IsNullOrWhiteSpace(x.PreviewUrl))
            .Select(x => new ItunesSongResult
            {
                TrackId = x.TrackId,
                TrackName = x.TrackName!,
                ArtistName = x.ArtistName!,
                CollectionName = x.CollectionName,
                PreviewUrl = x.PreviewUrl!,
                ArtworkUrl = x.ArtworkUrl100,
                TrackTimeMillis = x.TrackTimeMillis
            })
            .ToList();
    }
}
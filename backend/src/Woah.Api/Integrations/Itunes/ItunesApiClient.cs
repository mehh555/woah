using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Woah.Api.Domain;

namespace Woah.Api.Integrations.Itunes;

public class ItunesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _market;
    private readonly ILogger<ItunesApiClient> _logger;

    public ItunesApiClient(HttpClient httpClient, IOptions<ItunesSettings> settings, ILogger<ItunesApiClient> logger)
    {
        _httpClient = httpClient;
        _market = settings.Value.Market;
        _logger = logger;
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
            ["country"] = _market,
            ["media"] = "music",
            ["entity"] = "song",
            ["limit"] = GameConstants.ItunesSearchLimit.ToString()
        });

        _logger.LogDebug("iTunes search request: term={Term}", term);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ItunesSearchResponse>(cancellationToken: cancellationToken);

            var results = payload?.Results?
                .Where(x =>
                    x.TrackId > 0 &&
                    !string.IsNullOrWhiteSpace(x.TrackName) &&
                    !string.IsNullOrWhiteSpace(x.ArtistName) &&
                    !string.IsNullOrWhiteSpace(x.PreviewUrl))
                .ToList()
                ?? new List<ItunesTrackDto>();

            _logger.LogDebug("iTunes search returned {Count} results for term={Term}", results.Count, term);
            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "iTunes search failed for term={Term}", term);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "iTunes search timed out for term={Term}", term);
            throw;
        }
    }

    public async Task<ItunesTrackDto?> LookupSongAsync(long trackId, CancellationToken cancellationToken = default)
    {
        var url = QueryHelpers.AddQueryString("lookup", new Dictionary<string, string?>
        {
            ["id"] = trackId.ToString(),
            ["country"] = _market,
            ["entity"] = "song"
        });

        _logger.LogDebug("iTunes lookup request: trackId={TrackId}", trackId);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ItunesSearchResponse>(cancellationToken: cancellationToken);

            var result = payload?.Results?
                .FirstOrDefault(x =>
                    x.TrackId > 0 &&
                    !string.IsNullOrWhiteSpace(x.TrackName) &&
                    !string.IsNullOrWhiteSpace(x.ArtistName) &&
                    !string.IsNullOrWhiteSpace(x.PreviewUrl));

            if (result is null)
                _logger.LogWarning("iTunes lookup returned no valid result for trackId={TrackId}", trackId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "iTunes lookup failed for trackId={TrackId}", trackId);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "iTunes lookup timed out for trackId={TrackId}", trackId);
            throw;
        }
    }
}
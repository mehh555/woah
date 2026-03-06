using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Woah.Api.Infrastructure.Models;
using System.Text.Json;
namespace Woah.Api.Infrastructure.Auth
{
    public class SpotifyOAuthService
    {
        private readonly HttpClient _http;

        public SpotifyOAuthService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PlayerEntity> GetSpotifyPlayer(string accessToken)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.GetAsync("https://api.spotify.com/v1/me");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>()!;

            return new PlayerEntity
            {
                Nick = content.GetProperty("display_name").GetString(),
                AvatarUrl = content.TryGetProperty("images", out var images) && images.GetArrayLength() > 0
                            ? images[0].GetProperty("url").GetString()
                            : null,
                ExternalId = content.GetProperty("id").GetString(),
                Provider = AuthProvider.Spotify,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
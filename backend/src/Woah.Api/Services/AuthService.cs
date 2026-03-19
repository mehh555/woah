using Woah.Api.Infrastructure.Models; 
using Woah.Api.Infrastructure.Auth; 
using Microsoft.EntityFrameworkCore;

namespace Woah.Api.Services
{
    public class AuthService
{
    private readonly WoahDbContext _db;
    private readonly SpotifyOAuthService _spotify;

    public AuthService(WoahDbContext db, SpotifyOAuthService spotify)
    {
        _db = db;
        _spotify = spotify;
    }

    public async Task<PlayerEntity> LoginSpotify(string accessToken)
    {
        var spotifyPlayer = await _spotify.GetSpotifyPlayer(accessToken);

        var player = await _db.Players
            .FirstOrDefaultAsync(p => p.ExternalId == spotifyPlayer.ExternalId);

        if (player == null)
        {
            _db.Players.Add(spotifyPlayer);
            await _db.SaveChangesAsync();
            player = spotifyPlayer;
        }

        return player;
    }

    public async Task<PlayerEntity> LoginGuest()
    {
        var guestPlayer = new PlayerEntity
        {
            Nick = "Guest" + Guid.NewGuid().ToString().Substring(0, 8),
            Provider = AuthProvider.Guest,
            CreatedAt = DateTime.UtcNow
        };

        _db.Players.Add(guestPlayer);
        await _db.SaveChangesAsync();

        return guestPlayer;
    }
}
}
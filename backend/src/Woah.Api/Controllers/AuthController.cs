
using Microsoft.AspNetCore.Mvc;  
using Woah.Api.Infrastructure.Models; 
using Woah.Api.Infrastructure.WoahDbContext; 
using Woah.Api.Services; 
using Woah.Core.DTO;
namespace Woah.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;

        public AuthController(AuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("spotify")]
        public async Task<IActionResult> SpotifyLogin([FromBody] SpotifyLoginDto dto)
        {
            var player = await _auth.LoginSpotify(dto.AccessToken);
            return Ok(player);
        }

        [HttpPost("guest")]
        public async Task<IActionResult> GuestLogin()
        {
            var player = await _auth.LoginGuest();
            return Ok(player);
        }
    }
}
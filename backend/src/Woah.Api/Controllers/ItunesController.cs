using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Middleware;
using Woah.Api.Services.Playlist;
namespace Woah.Api.Controllers;

[ApiController]
[Route("api/itunes")]
public class ItunesController : ControllerBase
{
    private readonly ILobbyPlaylistService _lobbyPlaylistService;

    public ItunesController(ILobbyPlaylistService lobbyPlaylistService)
    {
        _lobbyPlaylistService = lobbyPlaylistService;
    }

    [HttpGet("search")]
    [EnableRateLimiting(RateLimitingConfiguration.ItunesSearch)]
    [ProducesResponseType(typeof(List<ItunesTrackSearchResultResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ItunesTrackSearchResultResponse>>> Search(
        [FromQuery] string term,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new { message = "Search term is required." });
        }

        var response = await _lobbyPlaylistService.SearchTracksAsync(term, cancellationToken);
        return Ok(response);
    }
}
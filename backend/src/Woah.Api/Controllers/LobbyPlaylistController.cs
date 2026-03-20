using Microsoft.AspNetCore.Mvc;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Services;

namespace Woah.Api.Controllers;

[ApiController]
[Route("api/lobbies/{lobbyCode}/playlist")]
public class LobbyPlaylistController : ControllerBase
{
    private readonly ILobbyPlaylistService _lobbyPlaylistService;

    public LobbyPlaylistController(ILobbyPlaylistService lobbyPlaylistService)
    {
        _lobbyPlaylistService = lobbyPlaylistService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetLobbyPlaylistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetLobbyPlaylistResponse>> GetPlaylist(
        [FromRoute] string lobbyCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lobbyPlaylistService.GetLobbyPlaylistAsync(lobbyCode, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("tracks")]
    [ProducesResponseType(typeof(GetLobbyPlaylistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetLobbyPlaylistResponse>> AddTrack(
        [FromRoute] string lobbyCode,
        [FromBody] AddLobbyTrackRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lobbyPlaylistService.AddTrackAsync(lobbyCode, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("tracks/{trackId:long}")]
    [ProducesResponseType(typeof(GetLobbyPlaylistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetLobbyPlaylistResponse>> RemoveTrack(
        [FromRoute] string lobbyCode,
        [FromRoute] long trackId,
        [FromQuery] Guid hostPlayerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lobbyPlaylistService.RemoveTrackAsync(
                lobbyCode,
                trackId,
                new RemoveLobbyTrackRequest { HostPlayerId = hostPlayerId },
                cancellationToken);

            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
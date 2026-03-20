using Microsoft.AspNetCore.Mvc;
using Woah.Api.Contracts.Lobbies;
using Woah.Api.Services;

namespace Woah.Api.Controllers;

[ApiController]
[Route("api/lobbies")]
public class LobbiesController : ControllerBase
{
    private readonly ILobbyService _lobbyService;

    public LobbiesController(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateLobbyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateLobbyResponse>> CreateLobby(
        [FromBody] CreateLobbyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _lobbyService.CreateLobbyAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{lobbyCode}/join")]
    [ProducesResponseType(typeof(JoinLobbyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JoinLobbyResponse>> JoinLobby(
        [FromRoute] string lobbyCode,
        [FromBody] JoinLobbyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lobbyService.JoinLobbyAsync(lobbyCode, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("{lobbyCode}")]
    [ProducesResponseType(typeof(GetLobbyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetLobbyResponse>> GetLobby(
        [FromRoute] string lobbyCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lobbyService.GetLobbyAsync(lobbyCode, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("{lobbyCode}/leave")]
    [ProducesResponseType(typeof(LeaveLobbyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LeaveLobbyResponse>> LeaveLobby(
        [FromRoute] string lobbyCode,
        [FromBody] LeaveLobbyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lobbyService.LeaveLobbyAsync(lobbyCode, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Services;

namespace Woah.Api.Controllers;

[ApiController]
[Route("api/lobbies/{lobbyCode}/session")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StartSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartSessionResponse>> StartSession(
        [FromRoute] string lobbyCode,
        [FromBody] StartSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sessionService.StartSessionAsync(lobbyCode, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
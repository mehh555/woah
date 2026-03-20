using Microsoft.AspNetCore.Mvc;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Services;

namespace Woah.Api.Controllers;

[ApiController]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("api/lobbies/{lobbyCode}/session")]
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

    [HttpGet("api/sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(GetSessionStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetSessionStateResponse>> GetSessionState(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sessionService.GetSessionStateAsync(sessionId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("api/sessions/{sessionId:guid}/answer")]
    [ProducesResponseType(typeof(SubmitAnswerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitAnswerResponse>> SubmitAnswer(
        [FromRoute] Guid sessionId,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sessionService.SubmitAnswerAsync(sessionId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("api/sessions/{sessionId:guid}/advance")]
    [ProducesResponseType(typeof(GetSessionStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetSessionStateResponse>> AdvanceSession(
        [FromRoute] Guid sessionId,
        [FromBody] AdvanceSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _sessionService.AdvanceSessionAsync(sessionId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
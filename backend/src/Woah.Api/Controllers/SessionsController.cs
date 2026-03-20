using Microsoft.AspNetCore.Mvc;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Services.Session;

namespace Woah.Api.Controllers;

[ApiController]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService) => _sessionService = sessionService;

    [HttpPost("api/lobbies/{lobbyCode}/session")]
    public async Task<ActionResult<StartSessionResponse>> StartSession(
        [FromRoute] string lobbyCode, [FromBody] StartSessionRequest request, CancellationToken ct)
        => Ok(await _sessionService.StartSessionAsync(lobbyCode, request, ct));

    [HttpGet("api/sessions/{sessionId:guid}")]
    public async Task<ActionResult<GetSessionStateResponse>> GetSessionState(
        [FromRoute] Guid sessionId, CancellationToken ct)
        => Ok(await _sessionService.GetSessionStateAsync(sessionId, ct));

    [HttpPost("api/sessions/{sessionId:guid}/answer")]
    public async Task<ActionResult<SubmitAnswerResponse>> SubmitAnswer(
        [FromRoute] Guid sessionId, [FromBody] SubmitAnswerRequest request, CancellationToken ct)
        => Ok(await _sessionService.SubmitAnswerAsync(sessionId, request, ct));

    [HttpPost("api/sessions/{sessionId:guid}/advance")]
    public async Task<ActionResult<GetSessionStateResponse>> AdvanceSession(
        [FromRoute] Guid sessionId, [FromBody] AdvanceSessionRequest request, CancellationToken ct)
        => Ok(await _sessionService.AdvanceSessionAsync(sessionId, request, ct));
}
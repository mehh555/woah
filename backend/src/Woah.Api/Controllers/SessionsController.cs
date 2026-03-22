using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Middleware;
using Woah.Api.Services.Session;
namespace Woah.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService) => _sessionService = sessionService;

    [HttpPost("~/api/lobbies/{lobbyCode}/session")]
    public async Task<ActionResult<StartSessionResponse>> StartSession(
        [FromRoute] string lobbyCode, [FromBody] StartSessionRequest request, CancellationToken ct)
        => Ok(await _sessionService.StartSessionAsync(lobbyCode, request, ct));

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<GetSessionStateResponse>> GetSessionState(
        [FromRoute] Guid sessionId, CancellationToken ct)
        => Ok(await _sessionService.GetSessionStateAsync(sessionId, ct));

    [HttpPost("{sessionId:guid}/answer")]
    [EnableRateLimiting(RateLimitingConfiguration.SubmitAnswer)]
    public async Task<ActionResult<SubmitAnswerResponse>> SubmitAnswer(
         [FromRoute] Guid sessionId, [FromBody] SubmitAnswerRequest request, CancellationToken ct)
        => Ok(await _sessionService.SubmitAnswerAsync(sessionId, request, ct));

    [HttpPost("{sessionId:guid}/advance")]
    public async Task<ActionResult<GetSessionStateResponse>> AdvanceSession(
        [FromRoute] Guid sessionId, [FromBody] AdvanceSessionRequest request, CancellationToken ct)
        => Ok(await _sessionService.AdvanceSessionAsync(sessionId, request, ct));

    [HttpPost("{sessionId:guid}/return-to-lobby")]
    public async Task<ActionResult<ReturnToLobbyResponse>> ReturnToLobby(
        [FromRoute] Guid sessionId, [FromBody] ReturnToLobbyRequest request, CancellationToken ct)
        => Ok(await _sessionService.ReturnToLobbyAsync(sessionId, request, ct));
}
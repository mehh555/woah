using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woah.Api.Contracts.Lobbies;
using Woah.Api.Middleware;
using Woah.Api.Services.Lobby;

namespace Woah.Api.Controllers;

[ApiController]
[Route("api/lobbies")]
public class LobbiesController : ControllerBase
{
    private readonly ILobbyService _lobbyService;

    public LobbiesController(ILobbyService lobbyService) => _lobbyService = lobbyService;

    [HttpPost]
    [EnableRateLimiting(RateLimitingConfiguration.CreateLobby)]
    public async Task<ActionResult<CreateLobbyResponse>> CreateLobby(
        [FromBody] CreateLobbyRequest request, CancellationToken ct)
        => Ok(await _lobbyService.CreateLobbyAsync(request, ct));

    [HttpPost("{lobbyCode}/join")]
    [EnableRateLimiting(RateLimitingConfiguration.JoinLobby)]
    public async Task<ActionResult<JoinLobbyResponse>> JoinLobby(
        [FromRoute] string lobbyCode, [FromBody] JoinLobbyRequest request, CancellationToken ct)
        => Ok(await _lobbyService.JoinLobbyAsync(lobbyCode, request, ct));

    [HttpGet("{lobbyCode}")]
    public async Task<ActionResult<GetLobbyResponse>> GetLobby(
        [FromRoute] string lobbyCode, CancellationToken ct)
        => Ok(await _lobbyService.GetLobbyAsync(lobbyCode, ct));

    [HttpPost("{lobbyCode}/leave")]
    public async Task<ActionResult<LeaveLobbyResponse>> LeaveLobby(
        [FromRoute] string lobbyCode, [FromBody] LeaveLobbyRequest request, CancellationToken ct)
        => Ok(await _lobbyService.LeaveLobbyAsync(lobbyCode, request, ct));
}
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
}
using Microsoft.AspNetCore.Mvc;
using Woah.Api.Contracts.Playlists;
using Woah.Api.Services.Playlist;

namespace Woah.Api.Controllers;

[ApiController]
[Route("api/lobbies/{lobbyCode}/playlist")]
public class LobbyPlaylistController : ControllerBase
{
    private readonly ILobbyPlaylistService _lobbyPlaylistService;

    public LobbyPlaylistController(ILobbyPlaylistService lobbyPlaylistService)
        => _lobbyPlaylistService = lobbyPlaylistService;

    [HttpGet]
    public async Task<ActionResult<GetLobbyPlaylistResponse>> GetPlaylist(
        [FromRoute] string lobbyCode, CancellationToken ct)
        => Ok(await _lobbyPlaylistService.GetLobbyPlaylistAsync(lobbyCode, ct));

    [HttpPost("tracks")]
    public async Task<ActionResult<GetLobbyPlaylistResponse>> AddTrack(
        [FromRoute] string lobbyCode, [FromBody] AddLobbyTrackRequest request, CancellationToken ct)
        => Ok(await _lobbyPlaylistService.AddTrackAsync(lobbyCode, request, ct));

    [HttpDelete("tracks/{trackId:long}")]
    public async Task<ActionResult<GetLobbyPlaylistResponse>> RemoveTrack(
        [FromRoute] string lobbyCode, [FromRoute] long trackId,
        [FromQuery] Guid hostPlayerId, CancellationToken ct)
        => Ok(await _lobbyPlaylistService.RemoveTrackAsync(
            lobbyCode, trackId, new RemoveLobbyTrackRequest { HostPlayerId = hostPlayerId }, ct));
}
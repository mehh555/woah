using Microsoft.AspNetCore.SignalR;
using Woah.Api.Hubs;

namespace Woah.Api.Services.Notifications;

public class GameNotifier : IGameNotifier
{
    private readonly IHubContext<GameHub> _hub;

    public GameNotifier(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public Task LobbyUpdated(string lobbyCode)
        => _hub.Clients.Group($"lobby:{lobbyCode}").SendAsync("LobbyUpdated");

    public Task SessionStarted(string lobbyCode, Guid sessionId)
        => _hub.Clients.Group($"lobby:{lobbyCode}").SendAsync("SessionStarted", new { sessionId });

    public Task SessionUpdated(Guid sessionId)
        => _hub.Clients.Group($"session:{sessionId}").SendAsync("SessionUpdated");

    public Task PlayerAnsweredCorrectly(Guid sessionId, Guid playerId, string nick, int points)
        => _hub.Clients.Group($"session:{sessionId}").SendAsync("PlayerAnsweredCorrectly", new { playerId, nick, points });
}
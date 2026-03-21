using Microsoft.AspNetCore.SignalR;
using Woah.Api.Infrastructure.Persistence;

namespace Woah.Api.Hubs;

public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;

    public GameHub(ILogger<GameHub> logger) => _logger = logger;

    public Task JoinLobby(string lobbyCode)
    {
        var code = lobbyCode.NormalizeCode();
        _logger.LogInformation("Connection {ConnectionId} joined lobby group {LobbyCode}", Context.ConnectionId, code);
        return Groups.AddToGroupAsync(Context.ConnectionId, $"lobby:{code}");
    }

    public Task LeaveLobby(string lobbyCode)
    {
        var code = lobbyCode.NormalizeCode();
        _logger.LogInformation("Connection {ConnectionId} left lobby group {LobbyCode}", Context.ConnectionId, code);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby:{code}");
    }

    public Task JoinSession(string sessionId)
    {
        _logger.LogInformation("Connection {ConnectionId} joined session group {SessionId}", Context.ConnectionId, sessionId);
        return Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");
    }

    public Task LeaveSession(string sessionId)
    {
        _logger.LogInformation("Connection {ConnectionId} left session group {SessionId}", Context.ConnectionId, sessionId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
            _logger.LogWarning(exception, "Connection {ConnectionId} disconnected with error", Context.ConnectionId);
        else
            _logger.LogDebug("Connection {ConnectionId} disconnected", Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }
}
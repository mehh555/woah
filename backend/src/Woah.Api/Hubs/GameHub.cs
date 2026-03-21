using Microsoft.AspNetCore.SignalR;
using Woah.Api.Infrastructure.Persistence;

namespace Woah.Api.Hubs;

public class GameHub : Hub
{
    public Task JoinLobby(string lobbyCode)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"lobby:{lobbyCode.NormalizeCode()}");

    public Task LeaveLobby(string lobbyCode)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby:{lobbyCode.NormalizeCode()}");

    public Task JoinSession(string sessionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");

    public Task LeaveSession(string sessionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
}
namespace Woah.Api.Services.Notifications;

public interface IGameNotifier
{
    Task LobbyUpdated(string lobbyCode);
    Task SessionStarted(string lobbyCode, Guid sessionId);
    Task SessionUpdated(Guid sessionId);
    Task PlayerAnsweredCorrectly(Guid sessionId, Guid playerId, string nick, int points);
}
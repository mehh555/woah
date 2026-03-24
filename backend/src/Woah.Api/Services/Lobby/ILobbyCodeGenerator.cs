using Woah.Api.Domain;

namespace Woah.Api.Services.Lobby;

public interface ILobbyCodeGenerator
{
    string Generate(int length = GameConstants.LobbyCodeLength);
}
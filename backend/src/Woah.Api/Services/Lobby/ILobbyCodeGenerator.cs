namespace Woah.Api.Services.Lobby;

public interface ILobbyCodeGenerator
{
    string Generate(int length = 6);
}
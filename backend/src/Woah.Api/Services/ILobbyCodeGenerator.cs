namespace Woah.Api.Services;

public interface ILobbyCodeGenerator
{
    string Generate(int length = 6);
}
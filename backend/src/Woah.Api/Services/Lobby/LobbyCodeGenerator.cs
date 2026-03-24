using System.Security.Cryptography;
using Woah.Api.Domain;

namespace Woah.Api.Services.Lobby;

public class LobbyCodeGenerator : ILobbyCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Generate(int length = GameConstants.LobbyCodeLength)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }
}
using System.Security.Cryptography;

namespace Woah.Api.Services;

public class LobbyCodeGenerator : ILobbyCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Generate(int length = 6)
    {
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(Alphabet.Length);
            chars[i] = Alphabet[index];
        }

        return new string(chars);
    }
}
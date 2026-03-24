using Moq;
using Woah.Api.Services.Lobby;
using Xunit;

namespace Woah.Api.Tests.Services.Lobby;

public class LobbyCodeGeneratorTests
{
    private readonly LobbyCodeGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsCodeWithCorrectLength()
    {
        var result = _generator.Generate(6);
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void Generate_ReturnsOnlyAllowedCharacters()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var result = _generator.Generate(100);

        foreach (var c in result)
        {
            Assert.Contains(c, alphabet);
        }
    }

    [Fact]
    public void Generate_ReturnsDifferentCodes()
    {
        var code1 = _generator.Generate(8);
        var code2 = _generator.Generate(8);

        Assert.NotEqual(code1, code2);
    }
}
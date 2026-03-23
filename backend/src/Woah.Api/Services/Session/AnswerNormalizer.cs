using System.Globalization;
using System.Text;

namespace Woah.Api.Services.Session;

public class AnswerNormalizer : IAnswerNormalizer
{
    private static readonly Dictionary<char, string> CharMap = new()
    {
        ['ł'] = "l",
        ['Ł'] = "l",
        ['$'] = "s",
        ['@'] = "a",
        ['!'] = "i",
        ['0'] = "o",
        ['1'] = "i",
        ['3'] = "e",
        ['4'] = "a",
        ['5'] = "s",
        ['7'] = "t",
        ['8'] = "b",
        ['ø'] = "o",
        ['đ'] = "d",
        ['ß'] = "ss",
        ['&'] = " ",
    };

    public string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lowered = value.Trim().ToLowerInvariant();

        var mapped = new StringBuilder(lowered.Length);
        foreach (var ch in lowered)
        {
            if (CharMap.TryGetValue(ch, out var replacement))
                mapped.Append(replacement);
            else
                mapped.Append(ch);
        }

        var decomposed = mapped.ToString().Normalize(NormalizationForm.FormD);
        var cleaned = new StringBuilder();

        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetter(ch) || char.IsWhiteSpace(ch))
                cleaned.Append(ch);
        }

        var result = cleaned.ToString().Normalize(NormalizationForm.FormC);
        var final = new StringBuilder();
        var lastWasSpace = false;

        foreach (var ch in result)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace) final.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                final.Append(ch);
                lastWasSpace = false;
            }
        }

        return final.ToString().Trim();
    }
}
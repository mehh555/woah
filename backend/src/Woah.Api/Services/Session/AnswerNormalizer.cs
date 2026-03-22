using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Woah.Api.Services.Session;

public class AnswerNormalizer : IAnswerNormalizer
{
    private static readonly Dictionary<char, char> ManualReplacements = new()
    {
        ['ł'] = 'l',
        ['đ'] = 'd',
        ['ø'] = 'o',
        ['ß'] = 's',
    };

    public string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lowered = value.Trim().ToLowerInvariant();

        // Handle characters that don't decompose in Unicode (e.g. ł → l)
        var preProcessed = new StringBuilder(lowered.Length);
        foreach (var ch in lowered)
        {
            preProcessed.Append(ManualReplacements.TryGetValue(ch, out var replacement) ? replacement : ch);
        }

        var decomposed = preProcessed.ToString().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                builder.Append(ch);
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }
}
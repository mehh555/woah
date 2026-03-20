using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Woah.Api.Services;

public class AnswerNormalizer : IAnswerNormalizer
{
    public string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim().ToLowerInvariant();
        var decomposed = trimmed.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder();

        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            {
                builder.Append(ch);
            }
        }

        var normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }
}
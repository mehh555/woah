namespace Woah.Api.Services.Session;

public class AnswerEvaluator : IAnswerEvaluator
{
    public bool IsCorrect(string normalizedGuess, string titleNorm, string artistNorm)
    {
        ReadOnlySpan<string> accepted =
        [
            titleNorm,
            artistNorm,
            $"{artistNorm} {titleNorm}",
            $"{titleNorm} {artistNorm}"
        ];

        foreach (var variant in accepted)
        {
            if (string.Equals(normalizedGuess, variant, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
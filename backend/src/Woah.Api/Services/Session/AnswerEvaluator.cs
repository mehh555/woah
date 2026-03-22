namespace Woah.Api.Services.Session;

public class AnswerEvaluator : IAnswerEvaluator
{
    public AnswerMatchResult Evaluate(string normalizedGuess, string titleNorm, string artistNorm)
    {
        if (string.Equals(normalizedGuess, $"{artistNorm} {titleNorm}", StringComparison.Ordinal) ||
            string.Equals(normalizedGuess, $"{titleNorm} {artistNorm}", StringComparison.Ordinal))
        {
            return new AnswerMatchResult(true, true);
        }

        var titleMatched = string.Equals(normalizedGuess, titleNorm, StringComparison.Ordinal);
        var artistMatched = string.Equals(normalizedGuess, artistNorm, StringComparison.Ordinal);

        return new AnswerMatchResult(titleMatched, artistMatched);
    }
}
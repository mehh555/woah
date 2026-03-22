namespace Woah.Api.Services.Session;

public record AnswerMatchResult(bool TitleMatched, bool ArtistMatched)
{
    public bool Any => TitleMatched || ArtistMatched;
}

public interface IAnswerEvaluator
{
    AnswerMatchResult Evaluate(string normalizedGuess, string titleNorm, string artistNorm);
}
namespace Woah.Api.Services.Session;

public interface IAnswerEvaluator
{
	bool IsCorrect(string normalizedGuess, string titleNorm, string artistNorm);
}
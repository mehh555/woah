namespace Woah.Api.Services.Session;

public interface IScoreCalculator
{
    int Calculate(int roundDurationSeconds, double elapsedSeconds);
}
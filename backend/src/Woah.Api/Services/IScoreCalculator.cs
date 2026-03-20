namespace Woah.Api.Services;

public interface IScoreCalculator
{
    int Calculate(int roundDurationSeconds, double elapsedSeconds);
}
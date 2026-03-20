namespace Woah.Api.Services;

public class LinearScoreCalculator : IScoreCalculator
{
    public int Calculate(int roundDurationSeconds, double elapsedSeconds)
    {
        if (roundDurationSeconds <= 0)
        {
            return 0;
        }

        if (elapsedSeconds < 0)
        {
            elapsedSeconds = 0;
        }

        if (elapsedSeconds >= roundDurationSeconds)
        {
            return 0;
        }

        var remainingRatio = (roundDurationSeconds - elapsedSeconds) / roundDurationSeconds;
        var points = (int)Math.Ceiling(100 * remainingRatio);

        return Math.Max(points, 1);
    }
}
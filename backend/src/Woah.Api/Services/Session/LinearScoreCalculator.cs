namespace Woah.Api.Services.Session;

public class LinearScoreCalculator : IScoreCalculator
{
    public int Calculate(int roundDurationSeconds, double elapsedSeconds)
    {
        if (roundDurationSeconds <= 0) return 0;

        elapsedSeconds = Math.Max(elapsedSeconds, 0);

        if (elapsedSeconds >= roundDurationSeconds) return 0;

        var ratio = (roundDurationSeconds - elapsedSeconds) / roundDurationSeconds;
        var points = (int)Math.Ceiling(100 * ratio);
        return Math.Max(points, 1);
    }
}
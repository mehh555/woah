namespace Woah.Api.Contracts.Sessions;

public class SubmitAnswerResponse
{
    public bool Accepted { get; set; }
    public bool IsCorrect { get; set; }
    public bool AlreadyAnswered { get; set; }
    public int PointsAwarded { get; set; }
    public string Message { get; set; } = default!;
}
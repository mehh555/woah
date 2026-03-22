namespace Woah.Api.Contracts.Sessions;

public class SubmitAnswerResponse
{
    public bool Accepted { get; set; }
    public bool IsCorrect { get; set; }
    public bool TitleCorrect { get; set; }
    public bool ArtistCorrect { get; set; }
    public bool AlreadyAnswered { get; set; }
    public int PointsAwarded { get; set; }
    public string Message { get; set; } = default!;

    public static SubmitAnswerResponse Rejected(string message) =>
        new() { Message = message };

    public static SubmitAnswerResponse AlreadyFullyAnswered() =>
        new() { Accepted = true, IsCorrect = true, TitleCorrect = true, ArtistCorrect = true, AlreadyAnswered = true, Message = "Player has already answered this round correctly." };
}
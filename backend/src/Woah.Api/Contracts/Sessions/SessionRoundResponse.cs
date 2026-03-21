namespace Woah.Api.Contracts.Sessions;

public class SessionRoundResponse
{
    public Guid RoundId { get; set; }
    public int RoundNo { get; set; }
    public string State { get; set; } = default!;
    public string PreviewUrl { get; set; } = default!;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime? RevealedAt { get; set; }
    public string? AnswerTitle { get; set; }
    public int AnswerCharCount { get; set; }
    public int CorrectAnswerCount { get; set; }
}
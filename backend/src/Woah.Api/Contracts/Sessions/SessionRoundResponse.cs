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
    public string? AnswerArtist { get; set; }
    public string? ArtworkUrl { get; set; }
    public string? ItunesUrl { get; set; }
    public string AnswerMask { get; set; } = default!;
    public int CorrectAnswerCount { get; set; }
    public List<Guid> CorrectPlayerIds { get; set; } = new();
}
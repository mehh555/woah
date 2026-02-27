namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class Round
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int RoundNo { get; set; }

    public Guid PlaylistId { get; set; }
    public int PlaylistItemNo { get; set; }

    public string PreviewUrl { get; set; } = null!;
    public string AnswerTitle { get; set; } = null!;
    public string AnswerNorm { get; set; } = null!;

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? RevealedAt { get; set; }

    public string State { get; set; } = "running";

    public GameSession Session { get; set; } = null!;
    public PlaylistTrack PlaylistTrack { get; set; } = null!;
    public ICollection<RoundCorrectAnswer> CorrectAnswers { get; set; } = new List<RoundCorrectAnswer>();
}
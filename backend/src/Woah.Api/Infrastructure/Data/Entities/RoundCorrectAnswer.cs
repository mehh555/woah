namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class RoundCorrectAnswer
{
    public Guid RoundId { get; set; }
    public Guid PlayerId { get; set; }

    public DateTimeOffset AnsweredAt { get; set; }
    public int Points { get; set; }

    public Round Round { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
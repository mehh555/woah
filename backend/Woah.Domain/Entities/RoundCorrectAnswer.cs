using System;

namespace Woah.Domain.Entities;

public class RoundCorrectAnswer
{
    public Guid RoundId { get; private set; }
    public Guid PlayerId { get; private set; }
    public DateTimeOffset AnsweredAt { get; private set; }
    public int Points { get; private set; }

    public Round Round { get; private set; } = null!;
    public Player Player { get; private set; } = null!;

    private RoundCorrectAnswer() { }

    public RoundCorrectAnswer(Guid roundId, Guid playerId, int points, DateTimeOffset answeredAt)
    {
        if (roundId == Guid.Empty) throw new ArgumentException("RoundId is required.", nameof(roundId));
        if (playerId == Guid.Empty) throw new ArgumentException("PlayerId is required.", nameof(playerId));
        if (points < 0) throw new ArgumentOutOfRangeException(nameof(points), "Points cannot be negative.");

        RoundId = roundId;
        PlayerId = playerId;
        Points = points;
        AnsweredAt = answeredAt;
    }

    public RoundCorrectAnswer(Guid roundId, Guid playerId, int points)
        : this(roundId, playerId, points, DateTimeOffset.UtcNow)
    {
    }
}
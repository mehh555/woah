using System;
using System.Collections.Generic;
using Woah.Domain.Enums;

namespace Woah.Domain.Entities;

public class Round
{
    public Guid Id { get; private set; }
    public long Version { get; private set; }
    public Guid SessionId { get; private set; }
    public int RoundNo { get; private set; }
    public Guid PlaylistId { get; private set; }
    public int PlaylistItemNo { get; private set; }
    public string PreviewUrl { get; private set; } = null!;
    public string AnswerTitle { get; private set; } = null!;
    public string AnswerNorm { get; private set; } = null!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public DateTimeOffset? RevealedAt { get; private set; }
    public RoundState State { get; private set; }

    public GameSession Session { get; private set; } = null!;
    public PlaylistTrack PlaylistTrack { get; private set; } = null!;

    private readonly List<RoundCorrectAnswer> _correctAnswers = [];
    public IReadOnlyCollection<RoundCorrectAnswer> CorrectAnswers => _correctAnswers.AsReadOnly();

    private Round() { }

    public Round(
        Guid sessionId,
        int roundNo,
        Guid playlistId,
        int playlistItemNo,
        string previewUrl,
        string answerTitle,
        string answerNorm,
        DateTimeOffset endsAt)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (playlistId == Guid.Empty) throw new ArgumentException("PlaylistId is required.", nameof(playlistId));
        if (roundNo <= 0) throw new ArgumentOutOfRangeException(nameof(roundNo), "RoundNo must be positive.");
        if (playlistItemNo <= 0) throw new ArgumentOutOfRangeException(nameof(playlistItemNo), "PlaylistItemNo must be positive.");
        if (string.IsNullOrWhiteSpace(previewUrl)) throw new ArgumentException("PreviewUrl is required.", nameof(previewUrl));
        if (string.IsNullOrWhiteSpace(answerTitle)) throw new ArgumentException("AnswerTitle is required.", nameof(answerTitle));
        if (string.IsNullOrWhiteSpace(answerNorm)) throw new ArgumentException("AnswerNorm is required.", nameof(answerNorm));
        if (endsAt <= DateTimeOffset.UtcNow) throw new ArgumentOutOfRangeException(nameof(endsAt), "EndsAt must be in the future.");

        Id = Guid.NewGuid();
        SessionId = sessionId;
        RoundNo = roundNo;
        PlaylistId = playlistId;
        PlaylistItemNo = playlistItemNo;
        PreviewUrl = previewUrl;
        AnswerTitle = answerTitle;
        AnswerNorm = answerNorm;
        StartedAt = DateTimeOffset.UtcNow;
        EndsAt = endsAt;
        State = RoundState.Running;
        Version = 0;
    }

    public void Reveal(DateTimeOffset now)
    {
        if (State != RoundState.Running)
            throw new InvalidOperationException("Round can be revealed only from Running state.");

        State = RoundState.Revealed;
        RevealedAt = now;
    }

    public void Reveal()
    {
        Reveal(DateTimeOffset.UtcNow);
    }

    public void Finish(DateTimeOffset now)
    {
        if (State == RoundState.Finished)
            return;

        if (State == RoundState.Running)
            throw new InvalidOperationException("Round must be revealed before finishing.");

        State = RoundState.Finished;
        if (RevealedAt is null)
            RevealedAt = now;
    }

    public void Finish()
    {
        Finish(DateTimeOffset.UtcNow);
    }
}
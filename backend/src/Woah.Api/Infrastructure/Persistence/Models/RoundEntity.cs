using System;
using System.Collections.Generic;
using Woah.Api.Domain;

namespace Woah.Api.Infrastructure.Persistence.Models;

public class RoundEntity
{
    public Guid RoundId { get; set; }
    public Guid SessionId { get; set; }
    public GameSessionEntity? Session { get; set; }

    public int RoundNo { get; set; }
    public Guid PlaylistId { get; set; }
    public int PlaylistItemNumber { get; set; }

    public string PreviewUrl { get; set; } = default!;
    public string AnswerTitle { get; set; } = default!;
    public string AnswerArtist { get; set; } = default!;
    public string AnswerNorm { get; set; } = default!;
    public string? ArtworkUrl { get; set; }
    public long? ItunesTrackId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime? RevealedAt { get; set; }
    public RoundState State { get; set; } = RoundState.Pending;

    public ICollection<RoundCorrectAnswerEntity> CorrectAnswers { get; set; } = new List<RoundCorrectAnswerEntity>();
}
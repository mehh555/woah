using System;
using System.Collections.Generic;

namespace Woah.Api.Infrastructure.Models;

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
    public string AnswerNorm { get; set; } = default!;

    public DateTime StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime? RevealedAt { get; set; }
    public string State { get; set; } = default!; 

    public ICollection<RoundCorrectAnswerEntity>? CorrectAnswers { get; set; }
}
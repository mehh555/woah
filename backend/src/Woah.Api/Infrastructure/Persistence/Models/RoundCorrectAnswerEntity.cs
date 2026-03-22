using System;

namespace Woah.Api.Infrastructure.Persistence.Models;

public class RoundCorrectAnswerEntity
{
    public Guid RoundId { get; set; }
    public RoundEntity? Round { get; set; }

    public Guid PlayerId { get; set; }
    public PlayerEntity? Player { get; set; }

    public DateTime AnsweredAt { get; set; }
    public int Points { get; set; }
    public bool GotTitle { get; set; }
    public bool GotArtist { get; set; }
}
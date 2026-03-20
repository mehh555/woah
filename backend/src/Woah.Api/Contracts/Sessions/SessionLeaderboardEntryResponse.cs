using System;

namespace Woah.Api.Contracts.Sessions;

public class SessionLeaderboardEntryResponse
{
    public Guid PlayerId { get; set; }
    public string Nick { get; set; } = default!;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
}
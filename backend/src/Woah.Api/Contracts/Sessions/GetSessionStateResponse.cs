namespace Woah.Api.Contracts.Sessions;

public class GetSessionStateResponse
{
    public Guid SessionId { get; set; }
    public Guid LobbyId { get; set; }
    public string LobbyStatus { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsFinished { get; set; }
    public int TotalRounds { get; set; }
    public int CompletedRounds { get; set; }
    public int RoundDurationSeconds { get; set; }
    public SessionRoundResponse? CurrentRound { get; set; }
    public List<SessionLeaderboardEntryResponse> Leaderboard { get; set; } = new();
}
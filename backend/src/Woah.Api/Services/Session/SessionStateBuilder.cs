using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public class SessionStateBuilder : ISessionStateBuilder
{
    private readonly WoahDbContext _dbContext;

    public SessionStateBuilder(WoahDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetSessionStateResponse> BuildAsync(GameSessionEntity session, CancellationToken ct)
    {
        var lobby = await _dbContext.Lobbies.Include(x => x.LobbyPlayers).FirstAsync(x => x.LobbyId == session.LobbyId, ct);
        var rounds = SessionProgressEngine.OrderedRounds(session);
        var settings = SessionSettings.Parse(session.SettingsJson);

        var current = rounds.FirstOrDefault(x => x.State == RoundState.Playing)
                   ?? rounds.FirstOrDefault(x => x.State == RoundState.Revealed);

        return new GetSessionStateResponse
        {
            SessionId = session.SessionId,
            LobbyId = session.LobbyId,
            LobbyStatus = lobby.Status.ToString(),
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            IsFinished = session.EndedAt is not null,
            TotalRounds = rounds.Count,
            CompletedRounds = rounds.Count(x => x.State == RoundState.Finished),
            RoundDurationSeconds = settings.RoundDurationSeconds,
            CurrentRound = current is null ? null : MapRound(current),
            Leaderboard = BuildLeaderboard(lobby.ActivePlayers(), rounds)
        };
    }

    private static SessionRoundResponse MapRound(RoundEntity round)
    {
        var correctIds = round.CorrectAnswers
            .Select(x => x.PlayerId)
            .ToList();

        return new SessionRoundResponse
        {
            RoundId = round.RoundId,
            RoundNo = round.RoundNo,
            State = round.State.ToString(),
            PreviewUrl = round.PreviewUrl,
            StartedAt = round.StartedAt,
            EndsAt = round.EndsAt,
            RevealedAt = round.RevealedAt,
            AnswerTitle = round.State is RoundState.Revealed or RoundState.Finished ? round.AnswerTitle : null,
            AnswerMask = BuildMask(round.AnswerTitle),
            CorrectAnswerCount = correctIds.Count,
            CorrectPlayerIds = correctIds
        };
    }

    private static string BuildMask(string title) =>
        new(title.Select(c => c == ' ' ? ' ' : '•').ToArray());

    private static List<SessionLeaderboardEntryResponse> BuildLeaderboard(
        List<LobbyPlayerEntity> players,
        List<RoundEntity> rounds) =>
        players
            .Select(p =>
            {
                var answers = rounds
                    .SelectMany(r => r.CorrectAnswers)
                    .Where(a => a.PlayerId == p.PlayerId)
                    .ToList();

                return new SessionLeaderboardEntryResponse
                {
                    PlayerId = p.PlayerId,
                    Nick = p.Nick,
                    Score = answers.Sum(a => a.Points),
                    CorrectAnswers = answers.Count
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.CorrectAnswers)
            .ThenBy(x => x.Nick)
            .ToList();
}
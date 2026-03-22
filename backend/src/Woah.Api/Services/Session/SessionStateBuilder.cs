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
        var answers = round.CorrectAnswers.ToList();
        var isRevealed = round.State is RoundState.Revealed or RoundState.Finished;

        return new SessionRoundResponse
        {
            RoundId = round.RoundId,
            RoundNo = round.RoundNo,
            State = round.State.ToString(),
            PreviewUrl = round.PreviewUrl,
            StartedAt = round.StartedAt,
            EndsAt = round.EndsAt,
            RevealedAt = round.RevealedAt,
            AnswerTitle = isRevealed ? round.AnswerTitle : null,
            AnswerArtist = isRevealed ? round.AnswerArtist : null,
            ArtworkUrl = isRevealed ? round.ArtworkUrl : null,
            ItunesUrl = isRevealed && round.ItunesTrackId.HasValue
                ? $"https://music.apple.com/pl/song/{round.ItunesTrackId.Value}"
                : null,
            AnswerTitleMask = BuildMask(round.AnswerTitle),
            AnswerArtistMask = BuildMask(round.AnswerArtist),
            CorrectAnswerCount = answers.Count,
            CorrectPlayerIds = answers.Select(x => x.PlayerId).ToList(),
            CorrectTitlePlayerIds = answers.Where(x => x.GotTitle).Select(x => x.PlayerId).ToList(),
            CorrectArtistPlayerIds = answers.Where(x => x.GotArtist).Select(x => x.PlayerId).ToList()
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
using Microsoft.EntityFrameworkCore;
using Woah.Api.Contracts;
using Woah.Api.Contracts.Sessions;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public class SessionStateBuilder : ISessionStateBuilder
{
    private readonly WoahDbContext _dbContext;
    private readonly ITrackTitleCleaner _cleaner;

    public SessionStateBuilder(WoahDbContext dbContext, ITrackTitleCleaner cleaner)
    {
        _dbContext = dbContext;
        _cleaner = cleaner;
    }

    public async Task<GetSessionStateResponse> BuildAsync(GameSessionEntity session, CancellationToken ct)
    {
        var lobby = await _dbContext.Lobbies.Include(x => x.LobbyPlayers).FirstAsync(x => x.LobbyId == session.LobbyId, ct);
        var rounds = SessionProgressEngine.OrderedRounds(session);
        var settings = SessionSettings.Parse(session.SettingsJson);

        var current = rounds.FirstOrDefault(x => x.State == RoundState.Playing)
                   ?? rounds.FirstOrDefault(x => x.State == RoundState.Revealed);

        var trackOwnerLookup = current?.ItunesTrackId is not null
            ? await _dbContext.PlaylistTracks
                .Where(pt => pt.PlaylistId == session.PlaylistId && pt.ItunesTrackId == current.ItunesTrackId)
                .Select(pt => (Guid?)pt.AddedByPlayerId)
                .FirstOrDefaultAsync(ct)
            : null;

        return new GetSessionStateResponse
        {
            SessionId = session.SessionId,
            LobbyId = session.LobbyId,
            LobbyStatus = lobby.Status.ToContract(),
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            IsFinished = session.EndedAt is not null,
            TotalRounds = rounds.Count,
            CompletedRounds = rounds.Count(x => x.State == RoundState.Finished),
            RoundDurationSeconds = settings.RoundDurationSeconds,
            CurrentRound = current is null ? null : MapRound(current, trackOwnerLookup),
            Leaderboard = BuildLeaderboard(lobby.ActivePlayers(), rounds)
        };
    }

    private SessionRoundResponse MapRound(RoundEntity round, Guid? addedByPlayerId)
    {
        var answers = round.CorrectAnswers.ToList();
        var isRevealed = round.State is RoundState.Revealed or RoundState.Finished;

        var cleanedTitle = _cleaner.CleanTitle(round.AnswerTitle);
        var mainArtist = _cleaner.ExtractMainArtist(round.AnswerArtist);

        return new SessionRoundResponse
        {
            RoundId = round.RoundId,
            RoundNo = round.RoundNo,
            State = round.State.ToContract(),
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
            AnswerTitleMask = BuildMask(cleanedTitle),
            AnswerArtistMask = BuildMask(mainArtist),
            CorrectAnswerCount = answers.Count,
            CorrectPlayerIds = answers.Select(x => x.PlayerId).ToList(),
            CorrectTitlePlayerIds = answers.Where(x => x.GotTitle).Select(x => x.PlayerId).ToList(),
            CorrectArtistPlayerIds = answers.Where(x => x.GotArtist).Select(x => x.PlayerId).ToList(),
            AddedByPlayerId = addedByPlayerId ?? Guid.Empty
        };
    }

    private static string BuildMask(string title) =>
        new(title.Select(c => c == ' ' ? ' ' : '•').ToArray());

    private static List<SessionLeaderboardEntryResponse> BuildLeaderboard(
        List<LobbyPlayerEntity> players,
        List<RoundEntity> rounds)
    {
        var statsByPlayer = rounds
            .SelectMany(r => r.CorrectAnswers)
            .GroupBy(a => a.PlayerId)
            .ToDictionary(
                g => g.Key,
                g => (Score: g.Sum(a => a.Points), Count: g.Count()));

        return players
            .Select(p =>
            {
                statsByPlayer.TryGetValue(p.PlayerId, out var stats);
                return new SessionLeaderboardEntryResponse
                {
                    PlayerId = p.PlayerId,
                    Nick = p.Nick,
                    Score = stats.Score,
                    CorrectAnswers = stats.Count
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.CorrectAnswers)
            .ThenBy(x => x.Nick)
            .ToList();
    }
}
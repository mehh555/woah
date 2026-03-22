using Microsoft.EntityFrameworkCore;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;
using Woah.Api.Services.Notifications;

namespace Woah.Api.Services.Session;

public class SessionProgressEngine : ISessionProgressEngine
{
    private readonly WoahDbContext _dbContext;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SessionProgressEngine> _logger;

    public SessionProgressEngine(
        WoahDbContext dbContext,
        IGameNotifier notifier,
        TimeProvider timeProvider,
        ILogger<SessionProgressEngine> logger)
    {
        _dbContext = dbContext;
        _notifier = notifier;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task EnsurePlayingToRevealedAsync(GameSessionEntity session, CancellationToken ct)
    {
        if (session.EndedAt is not null) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var rounds = OrderedRounds(session);

        var playing = rounds.FirstOrDefault(x => x.State == RoundState.Playing);
        if (playing?.EndsAt is not null && now >= playing.EndsAt.Value)
        {
            playing.State = RoundState.Revealed;
            playing.RevealedAt = playing.EndsAt.Value;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Round {RoundNo} auto-revealed (timer expired) in session {SessionId}",
                playing.RoundNo, session.SessionId);

            await _notifier.SessionUpdated(session.SessionId);
        }
    }

    public async Task AdvanceFromRevealedAsync(
        GameSessionEntity session,
        LobbyEntity lobby,
        RoundEntity revealed,
        List<RoundEntity> orderedRounds,
        CancellationToken ct)
    {
        var settings = SessionSettings.Parse(session.SettingsJson);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        revealed.State = RoundState.Finished;

        var next = orderedRounds.FirstOrDefault(x => x.State == RoundState.Pending);

        if (next is null)
        {
            session.EndedAt = now;
            lobby.Status = LobbyStatus.Finished;
            _logger.LogInformation("Session {SessionId} finished — no more rounds", session.SessionId);
        }
        else
        {
            next.State = RoundState.Playing;
            next.StartedAt = now;
            next.EndsAt = now.AddSeconds(settings.RoundDurationSeconds);
            next.RevealedAt = null;
            _logger.LogInformation("Advanced to round {RoundNo} in session {SessionId}",
                next.RoundNo, session.SessionId);
        }

        await _dbContext.SaveChangesAsync(ct);
        await _notifier.SessionUpdated(session.SessionId);
    }

    public static List<RoundEntity> OrderedRounds(GameSessionEntity session) =>
        session.Rounds
            .OrderBy(x => x.RoundNo)
            .ToList();
}
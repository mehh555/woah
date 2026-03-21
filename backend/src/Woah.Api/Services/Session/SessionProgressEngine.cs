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

    public SessionProgressEngine(WoahDbContext dbContext, IGameNotifier notifier)
    {
        _dbContext = dbContext;
        _notifier = notifier;
    }

    public async Task EnsurePlayingToRevealedAsync(GameSessionEntity session, CancellationToken ct)
    {
        if (session.EndedAt is not null) return;

        var now = DateTime.UtcNow;
        var rounds = OrderedRounds(session);

        var playing = rounds.FirstOrDefault(x => x.State == RoundState.Playing);
        if (playing?.EndsAt is not null && now >= playing.EndsAt.Value)
        {
            playing.State = RoundState.Revealed;
            playing.RevealedAt = playing.EndsAt.Value;
            await _dbContext.SaveChangesAsync(ct);
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

        revealed.State = RoundState.Finished;

        var next = orderedRounds.FirstOrDefault(x => x.State == RoundState.Pending);

        if (next is null)
        {
            session.EndedAt = DateTime.UtcNow;
            lobby.Status = LobbyStatus.Finished;
        }
        else
        {
            next.State = RoundState.Playing;
            next.StartedAt = DateTime.UtcNow;
            next.EndsAt = next.StartedAt.AddSeconds(settings.RoundDurationSeconds);
            next.RevealedAt = null;
        }

        await _dbContext.SaveChangesAsync(ct);
        await _notifier.SessionUpdated(session.SessionId);
    }

    public static List<RoundEntity> OrderedRounds(GameSessionEntity session) =>
        (session.Rounds ?? new List<RoundEntity>())
            .OrderBy(x => x.RoundNo)
            .ToList();
}
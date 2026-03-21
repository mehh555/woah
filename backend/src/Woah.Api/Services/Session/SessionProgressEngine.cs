using Microsoft.EntityFrameworkCore;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public class SessionProgressEngine : ISessionProgressEngine
{
    private readonly WoahDbContext _dbContext;

    public SessionProgressEngine(WoahDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureProgressAsync(GameSessionEntity session, CancellationToken ct)
    {
        if (session.EndedAt is not null) return;

        var rounds = OrderedRounds(session);
        var settings = SessionSettings.Parse(session.SettingsJson);
        LobbyEntity? lobby = null;

        while (session.EndedAt is null)
        {
            var now = DateTime.UtcNow;

            var playing = rounds.FirstOrDefault(x => x.State == RoundState.Playing);
            if (playing?.EndsAt is not null && now >= playing.EndsAt.Value)
            {
                playing.State = RoundState.Revealed;
                playing.RevealedAt = playing.EndsAt.Value;
                await _dbContext.SaveChangesAsync(ct);
            }

            var revealed = rounds.FirstOrDefault(x => x.State == RoundState.Revealed);
            if (revealed?.RevealedAt is null) break;
            if (now < revealed.RevealedAt.Value.AddSeconds(settings.RevealDurationSeconds)) break;

            lobby ??= await _dbContext.Lobbies.FirstAsync(x => x.LobbyId == session.LobbyId, ct);
            await AdvanceFromRevealedAsync(session, lobby, revealed, rounds, settings, ct);
        }
    }

    public async Task AdvanceFromRevealedAsync(
        GameSessionEntity session,
        LobbyEntity lobby,
        RoundEntity revealed,
        List<RoundEntity> orderedRounds,
        SessionSettings settings,
        CancellationToken ct)
    {
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
    }

    public static List<RoundEntity> OrderedRounds(GameSessionEntity session) =>
        (session.Rounds ?? new List<RoundEntity>())
            .Ord
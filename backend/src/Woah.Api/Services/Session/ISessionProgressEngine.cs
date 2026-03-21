using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public interface ISessionProgressEngine
{
    Task EnsurePlayingToRevealedAsync(GameSessionEntity session, CancellationToken ct);
    Task AdvanceFromRevealedAsync(GameSessionEntity session, LobbyEntity lobby, RoundEntity revealed, List<RoundEntity> orderedRounds, CancellationToken ct);
}
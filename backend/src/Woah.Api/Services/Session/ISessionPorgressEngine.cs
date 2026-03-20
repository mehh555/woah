using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Services.Session;

public interface ISessionProgressEngine
{
    Task EnsureProgressAsync(GameSessionEntity session, CancellationToken ct);
    Task AdvanceFromRevealedAsync(GameSessionEntity session, LobbyEntity lobby, RoundEntity revealed, List<RoundEntity> orderedRounds, SessionSettings settings, CancellationToken ct);
}
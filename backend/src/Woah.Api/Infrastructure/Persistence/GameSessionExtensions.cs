using Microsoft.EntityFrameworkCore;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Infrastructure.Persistence;

internal static class GameSessionExtensions
{
    public static async Task<GameSessionEntity> GetSessionWithRoundsAsync(
        this DbSet<GameSessionEntity> sessions, Guid sessionId, CancellationToken ct)
        => await sessions
            .Include(x => x.Rounds).ThenInclude(x => x.CorrectAnswers)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, ct)
        ?? throw new NotFoundException("Session not found.");
}

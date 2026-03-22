using Microsoft.EntityFrameworkCore;
using Woah.Api.Exceptions;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Infrastructure.Persistence;

internal static class LobbyEntityExtensions
{
    public static string NormalizeCode(this string lobbyCode)
        => lobbyCode.Trim().ToUpperInvariant();

    public static List<LobbyPlayerEntity> ActivePlayers(this LobbyEntity lobby)
        => lobby.LobbyPlayers
            .Where(x => x.LeftAt == null)
            .OrderBy(x => x.JoinedAt)
            .ToList();

    public static async Task<LobbyEntity> GetLobbyWithPlayersAsync(
        this DbSet<LobbyEntity> lobbies, string normalizedCode, CancellationToken ct)
        => await lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, ct)
        ?? throw new NotFoundException("Lobby not found.");

    public static async Task<LobbyEntity> GetLobbyWithPlayersForUpdateAsync(
        this WoahDbContext dbContext, string normalizedCode, CancellationToken ct)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Lobbies\" WHERE \"Code\" = {normalizedCode} FOR UPDATE", ct);

        return await dbContext.Lobbies
            .Include(x => x.LobbyPlayers)
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, ct)
            ?? throw new NotFoundException("Lobby not found.");
    }
}
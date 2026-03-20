using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Domain;

internal static class LobbyExtensions
{
    public static string NormalizeCode(this string lobbyCode)
        => lobbyCode.Trim().ToUpperInvariant();

    public static List<LobbyPlayerEntity> ActivePlayers(this LobbyEntity lobby)
        => (lobby.LobbyPlayers ?? new List<LobbyPlayerEntity>())
            .Where(x => x.LeftAt == null)
            .OrderBy(x => x.JoinedAt)
            .ToList();
}
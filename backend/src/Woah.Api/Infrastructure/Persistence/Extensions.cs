using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Infrastructure.Persistence;

internal static class Extensions
{
    public static string NormalizeCode(this string lobbyCode)
        => lobbyCode.Trim().ToUpperInvariant();

    public static List<LobbyPlayerEntity> ActivePlayers(this LobbyEntity lobby)
        => lobby.LobbyPlayers
            .Where(x => x.LeftAt == null)
            .OrderBy(x => x.JoinedAt)
            .ToList();
}
using System.Text;

namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class GameSession
{
    public Guid Id { get; set; }
    public Guid LobbyId { get; set; }
    public Guid PlaylistId { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public string SettingsJson { get; set; } = "{}";

    public Lobby Lobby { get; set; } = null!;
    public Playlist Playlist { get; set; } = null!;
    public ICollection<Round> Rounds { get; set; } = new List<Round>();
}
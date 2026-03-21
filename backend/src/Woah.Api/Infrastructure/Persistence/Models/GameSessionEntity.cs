using System;
using System.Collections.Generic;

namespace Woah.Api.Infrastructure.Persistence.Models;

public class GameSessionEntity
{
    public Guid SessionId { get; set; }
    public Guid LobbyId { get; set; }
    public Guid PlaylistId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string SettingsJson { get; set; } = default!;

    public LobbyEntity Lobby { get; set; } = default!;
    public PlaylistEntity Playlist { get; set; } = default!;
    public ICollection<RoundEntity> Rounds { get; set; } = new List<RoundEntity>();
}
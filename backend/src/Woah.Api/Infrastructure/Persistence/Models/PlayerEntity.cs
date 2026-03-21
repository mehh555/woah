using System;
using System.Collections.Generic;

namespace Woah.Api.Infrastructure.Persistence.Models;

public class PlayerEntity
{
    public Guid PlayerId { get; set; }
    public string Nick { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public ICollection<LobbyPlayerEntity> LobbyMemberships { get; set; } = new List<LobbyPlayerEntity>();
    public ICollection<PlaylistEntity> Playlists { get; set; } = new List<PlaylistEntity>();
}
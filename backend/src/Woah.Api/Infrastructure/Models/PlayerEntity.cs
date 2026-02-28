using System;
using System.Collections.Generic;

namespace Woah.Api.Infrastructure.Models;


public class PlayerEntity
{
    
    public Guid PlayerId { get; set; }
    public string? Nick { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AvatarUrl { get; set; } 

    public ICollection<LobbyPlayerEntity> LobbyMemberships { get; set; } = new List<LobbyPlayerEntity>();
    public ICollection<PlaylistEntity> Playlists { get; set; } = new List<PlaylistEntity>();
}
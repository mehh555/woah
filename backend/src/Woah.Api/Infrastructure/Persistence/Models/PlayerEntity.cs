using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Woah.Api.Infrastructure.Persistence.Models;

public enum AuthProvider
{
    Guest,
    Spotify
}

public class PlayerEntity
{
    public Guid PlayerId { get; set; }
    public string? Nick { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AvatarUrl { get; set; }

    [NotMapped]
    public AuthProvider Provider { get; set; }

    [NotMapped]
    public string? ExternalId { get; set; }

    public ICollection<LobbyPlayerEntity> LobbyMemberships { get; set; } = new List<LobbyPlayerEntity>();
    public ICollection<PlaylistEntity> Playlists { get; set; } = new List<PlaylistEntity>();
}
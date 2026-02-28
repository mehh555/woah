using System;

namespace Woah.Api.Infrastructure.Models;

public class LobbyPlayerEntity
{
    public Guid LobbyId { get; set; }
    public LobbyEntity? Lobby { get; set; }

    public Guid PlayerId { get; set; }
    public PlayerEntity? Player { get; set; }

    public string Nick { get; set; } = default!; 
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}
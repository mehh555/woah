using System;
using System.Collections.Generic;

namespace Woah.Api.Infrastructure.Persistence.Models;

public class LobbyEntity
{
    public Guid LobbyId { get; set; }
    public string Code { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public Guid HostPlayerId { get; set; }

    public PlayerEntity? HostPlayer { get; set; }
    public ICollection<LobbyPlayerEntity>? LobbyPlayers { get; set; }
    public int MaxPlayers { get; set; } = 10;
}
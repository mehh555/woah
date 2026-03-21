using System;
using System.Collections.Generic;
using Woah.Api.Domain;

namespace Woah.Api.Infrastructure.Persistence.Models;

public class LobbyEntity
{
    public Guid LobbyId { get; set; }
    public string Code { get; set; } = default!;
    public LobbyStatus Status { get; set; } = LobbyStatus.Waiting;
    public DateTime CreatedAt { get; set; }
    public Guid HostPlayerId { get; set; }

    public PlayerEntity? HostPlayer { get; set; }
    public ICollection<LobbyPlayerEntity> LobbyPlayers { get; set; } = new List<LobbyPlayerEntity>();
    public int MaxPlayers { get; set; } = 10;
}
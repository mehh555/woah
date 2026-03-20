using System;

namespace Woah.Api.Contracts.Sessions;

public class StartSessionResponse
{
    public Guid SessionId { get; set; }
    public Guid LobbyId { get; set; }
    public Guid PlaylistId { get; set; }
    public Guid HostPlayerId { get; set; }
    public DateTime StartedAt { get; set; }
    public string LobbyStatus { get; set; } = default!;
    public string SettingsJson { get; set; } = default!;
}
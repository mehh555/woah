using System;

namespace Woah.Api.Contracts.Sessions;

public class StartSessionRequest
{
    public Guid HostPlayerId { get; set; }
    public Guid PlaylistId { get; set; }
    public int RoundDurationSeconds { get; set; } = 15;
}
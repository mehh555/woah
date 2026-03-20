using System;

namespace Woah.Api.Contracts.Sessions;

public class AdvanceSessionRequest
{
    public Guid HostPlayerId { get; set; }
}
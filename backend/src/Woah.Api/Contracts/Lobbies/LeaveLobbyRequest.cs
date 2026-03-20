using System;

namespace Woah.Api.Contracts.Lobbies;

public class LeaveLobbyRequest
{
    public Guid PlayerId { get; set; }
}
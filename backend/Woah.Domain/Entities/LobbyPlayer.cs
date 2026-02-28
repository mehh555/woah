using System;

namespace Woah.Domain.Entities;

public class LobbyPlayer
{
    public Guid LobbyId { get; private set; }
    public Guid PlayerId { get; private set; }
    public string Nick { get; private set; } = null!;
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? LeftAt { get; private set; }

    public Lobby Lobby { get; private set; } = null!;
    public Player Player { get; private set; } = null!;

    private LobbyPlayer() { }

    public LobbyPlayer(Guid lobbyId, Guid playerId, string nick)
    {
        if (lobbyId == Guid.Empty) throw new ArgumentException("LobbyId is required.", nameof(lobbyId));
        if (playerId == Guid.Empty) throw new ArgumentException("PlayerId is required.", nameof(playerId));
        if (string.IsNullOrWhiteSpace(nick)) throw new ArgumentException("Nick is required.", nameof(nick));

        LobbyId = lobbyId;
        PlayerId = playerId;
        Nick = nick;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive => LeftAt is null;

    public void Leave(DateTimeOffset now)
    {
        if (LeftAt is not null)
            return;

        LeftAt = now;
    }

    public void Leave()
    {
        Leave(DateTimeOffset.UtcNow);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Woah.Domain.Enums;

namespace Woah.Domain.Entities;

public class Lobby
{
    public Guid Id { get; private set; }
    public long Version { get; private set; }
    public string Code { get; private set; } = null!;
    public LobbyStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid HostPlayerId { get; private set; }
    public short MaxPlayers { get; private set; }

    public Player HostPlayer { get; private set; } = null!;

    private readonly List<LobbyPlayer> _players = [];
    public IReadOnlyCollection<LobbyPlayer> Players => _players.AsReadOnly();

    private Lobby() { }

    public Lobby(string code, Guid hostPlayerId, short maxPlayers = 10)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (hostPlayerId == Guid.Empty) throw new ArgumentException("HostPlayerId is required.", nameof(hostPlayerId));
        if (maxPlayers < 2) throw new ArgumentOutOfRangeException(nameof(maxPlayers), "MaxPlayers must be at least 2.");

        Id = Guid.NewGuid();
        Code = code;
        HostPlayerId = hostPlayerId;
        MaxPlayers = maxPlayers;
        Status = LobbyStatus.Waiting;
        CreatedAt = DateTimeOffset.UtcNow;
        Version = 0;
    }

    public int ActivePlayersCount
    {
        get
        {
            var active = _players.Count(p => p.LeftAt is null);
            var hostIncluded = _players.Any(p => p.PlayerId == HostPlayerId && p.LeftAt is null);
            return hostIncluded ? active : active + 1;
        }
    }

    public void StartGame()
    {
        if (Status != LobbyStatus.Waiting)
            throw new InvalidOperationException("Lobby can be started only from Waiting status.");

        if (ActivePlayersCount < 2)
            throw new InvalidOperationException("Zbyt mało graczy, aby rozpocząć grę.");

        Status = LobbyStatus.Playing;
    }

    public void FinishGame()
    {
        if (Status == LobbyStatus.Finished)
            return;

        if (Status != LobbyStatus.Playing)
            throw new InvalidOperationException("Lobby can be finished only from Playing status.");

        Status = LobbyStatus.Finished;
    }
}
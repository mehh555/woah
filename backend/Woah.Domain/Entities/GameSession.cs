using System;
using System.Collections.Generic;

namespace Woah.Domain.Entities;

public class GameSession
{
    public Guid Id { get; private set; }
    public long Version { get; private set; }
    public Guid LobbyId { get; private set; }
    public Guid PlaylistId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public string SettingsJson { get; private set; } = null!;

    public Lobby Lobby { get; private set; } = null!;
    public Playlist Playlist { get; private set; } = null!;

    private readonly List<Round> _rounds = [];
    public IReadOnlyCollection<Round> Rounds => _rounds.AsReadOnly();

    private GameSession() { }

    public GameSession(Guid lobbyId, Guid playlistId, string settingsJson = "{}")
    {
        if (lobbyId == Guid.Empty) throw new ArgumentException("LobbyId is required.", nameof(lobbyId));
        if (playlistId == Guid.Empty) throw new ArgumentException("PlaylistId is required.", nameof(playlistId));
        if (string.IsNullOrWhiteSpace(settingsJson)) throw new ArgumentException("SettingsJson is required.", nameof(settingsJson));

        Id = Guid.NewGuid();
        LobbyId = lobbyId;
        PlaylistId = playlistId;
        SettingsJson = settingsJson;
        StartedAt = DateTimeOffset.UtcNow;
        Version = 0;
    }

    public bool IsEnded => EndedAt is not null;

    public void EndSession(DateTimeOffset now)
    {
        if (EndedAt is not null)
            return;

        EndedAt = now;
    }

    public void EndSession()
    {
        EndSession(DateTimeOffset.UtcNow);
    }
}
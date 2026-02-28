using System;
using System.Collections.Generic;

namespace Woah.Domain.Entities;

public class Playlist
{
    public Guid Id { get; private set; }
    public long Version { get; private set; }
    public Guid OwnerPlayerId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Market { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public Player OwnerPlayer { get; private set; } = null!;

    private readonly List<PlaylistTrack> _tracks = [];
    public IReadOnlyCollection<PlaylistTrack> Tracks => _tracks.AsReadOnly();

    private Playlist() { }

    public Playlist(Guid ownerPlayerId, string name, string market = "PL")
    {
        if (ownerPlayerId == Guid.Empty) throw new ArgumentException("OwnerPlayerId is required.", nameof(ownerPlayerId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(market)) throw new ArgumentException("Market is required.", nameof(market));

        Id = Guid.NewGuid();
        OwnerPlayerId = ownerPlayerId;
        Name = name;
        Market = market;
        CreatedAt = DateTimeOffset.UtcNow;
        Version = 0;
    }
}
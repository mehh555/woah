using System;

namespace Woah.Domain.Entities;

public class Player
{
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Player() { }

    public Player(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));

        Id = id;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
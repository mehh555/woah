namespace Woah.Api.Infrastructure.Data.Entities;

public sealed class Player
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
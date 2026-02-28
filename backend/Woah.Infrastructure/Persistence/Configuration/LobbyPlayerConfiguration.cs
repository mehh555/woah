using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence.Configurations;

public class LobbyPlayerConfiguration : IEntityTypeConfiguration<LobbyPlayer>
{
    public void Configure(EntityTypeBuilder<LobbyPlayer> builder)
    {
        builder.HasKey(x => new { x.LobbyId, x.PlayerId });

        builder.Property(x => x.Nick)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.JoinedAt).IsRequired();

        builder.HasIndex(x => new { x.LobbyId, x.Nick }).IsUnique();
        builder.HasIndex(x => x.PlayerId);

        builder.HasOne(x => x.Lobby)
            .WithMany(l => l.Players)
            .HasForeignKey(x => x.LobbyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
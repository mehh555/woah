using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence.Configurations;

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version)
            .IsRequired()
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.Property(x => x.SettingsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.StartedAt).IsRequired();

        builder.HasOne(x => x.Lobby)
            .WithMany()
            .HasForeignKey(x => x.LobbyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Playlist)
            .WithMany()
            .HasForeignKey(x => x.PlaylistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Rounds)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.LobbyId);
        builder.HasIndex(x => x.PlaylistId);
    }
}
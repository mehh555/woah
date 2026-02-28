using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence.Configurations;

public class LobbyConfiguration : IEntityTypeConfiguration<Lobby>
{
    public void Configure(EntityTypeBuilder<Lobby> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version)
            .IsRequired()
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.Property(x => x.Code)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.MaxPlayers).IsRequired();

        builder.HasOne(x => x.HostPlayer)
            .WithMany()
            .HasForeignKey(x => x.HostPlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Players)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
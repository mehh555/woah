using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence.Configurations;

public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version)
            .IsRequired()
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Market)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.OwnerPlayer)
            .WithMany()
            .HasForeignKey(x => x.OwnerPlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Tracks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
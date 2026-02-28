using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence.Configurations;

public class PlaylistTrackConfiguration : IEntityTypeConfiguration<PlaylistTrack>
{
    public void Configure(EntityTypeBuilder<PlaylistTrack> builder)
    {
        builder.HasKey(x => new { x.PlaylistId, x.ItemNo });

        builder.Property(x => x.TrackJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.PreviewUrl)
            .HasMaxLength(512);

        builder.Property(x => x.SpotifyTrackId)
            .HasMaxLength(64);

        builder.Property(x => x.SpotifyUrl)
            .HasMaxLength(512);

        builder.Property(x => x.InvalidReason)
            .HasMaxLength(256);

        builder.Property(x => x.IsValid).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.PlaylistId, x.SpotifyTrackId })
            .IsUnique()
            .HasFilter("spotify_track_id IS NOT NULL");

        builder.HasOne(x => x.Playlist)
            .WithMany(p => p.Tracks)
            .HasForeignKey(x => x.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
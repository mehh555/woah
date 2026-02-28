using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence.Configurations;

public class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version)
            .IsRequired()
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.Property(x => x.State)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.RoundNo).IsRequired();

        builder.Property(x => x.PreviewUrl)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.AnswerTitle)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.AnswerNorm)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired();

        builder.HasOne(x => x.Session)
            .WithMany(s => s.Rounds)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PlaylistTrack)
            .WithMany()
            .HasForeignKey(x => new { x.PlaylistId, x.PlaylistItemNo })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SessionId, x.RoundNo }).IsUnique();

        builder.Navigation(x => x.CorrectAnswers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
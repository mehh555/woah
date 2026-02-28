using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence.Configurations;

public class RoundCorrectAnswerConfiguration : IEntityTypeConfiguration<RoundCorrectAnswer>
{
    public void Configure(EntityTypeBuilder<RoundCorrectAnswer> builder)
    {
        builder.HasKey(x => new { x.RoundId, x.PlayerId });

        builder.Property(x => x.AnsweredAt).IsRequired();
        builder.Property(x => x.Points).IsRequired();

        builder.HasOne(x => x.Round)
            .WithMany(r => r.CorrectAnswers)
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PlayerId);
    }
}
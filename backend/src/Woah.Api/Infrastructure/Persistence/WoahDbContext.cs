using Microsoft.EntityFrameworkCore;
using Woah.Api.Domain;
using Woah.Api.Infrastructure.Persistence.Models;

namespace Woah.Api.Infrastructure.Persistence;

public class WoahDbContext : DbContext
{
    public WoahDbContext(DbContextOptions<WoahDbContext> options) : base(options) { }

    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<LobbyEntity> Lobbies => Set<LobbyEntity>();
    public DbSet<LobbyPlayerEntity> LobbyPlayers => Set<LobbyPlayerEntity>();
    public DbSet<PlaylistEntity> Playlists => Set<PlaylistEntity>();
    public DbSet<GameSessionEntity> GameSessions => Set<GameSessionEntity>();
    public DbSet<RoundEntity> Rounds => Set<RoundEntity>();
    public DbSet<RoundCorrectAnswerEntity> RoundCorrectAnswers => Set<RoundCorrectAnswerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerEntity>(e =>
        {
            e.HasKey(p => p.PlayerId);

            e.Property(p => p.Nick)
                .IsRequired()
                .HasMaxLength(30);
        });

        modelBuilder.Entity<LobbyEntity>(e =>
        {
            e.HasKey(l => l.LobbyId);

            e.HasIndex(l => l.Code)
                .IsUnique();

            e.Property(l => l.Code)
                .IsRequired()
                .HasMaxLength(10);

            e.Property(l => l.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>();

            e.HasOne(l => l.HostPlayer)
                .WithMany()
                .HasForeignKey(l => l.HostPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t => t.HasCheckConstraint("CK_Lobby_MaxPlayers", "\"MaxPlayers\" >= 2 AND \"MaxPlayers\" <= 20"));
        });

        modelBuilder.Entity<LobbyPlayerEntity>(e =>
        {
            e.HasKey(lp => new { lp.LobbyId, lp.PlayerId });

            e.HasIndex(lp => new { lp.LobbyId, lp.Nick })
                .IsUnique()
                .HasFilter("\"LeftAt\" IS NULL");

            e.Property(lp => lp.Nick)
                .IsRequired()
                .HasMaxLength(30);

            e.HasOne(lp => lp.Lobby)
                .WithMany(l => l.LobbyPlayers)
                .HasForeignKey(lp => lp.LobbyId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(lp => lp.Player)
                .WithMany(p => p.LobbyMemberships)
                .HasForeignKey(lp => lp.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlaylistEntity>(e =>
        {
            e.HasKey(pl => pl.PlaylistId);

            e.Property(pl => pl.Name)
                .IsRequired()
                .HasMaxLength(100);

            e.Property(pl => pl.Market)
                .IsRequired()
                .HasMaxLength(5);
        });

        modelBuilder.Entity<GameSessionEntity>(e =>
        {
            e.HasKey(gs => gs.SessionId);

            e.Property(gs => gs.SettingsJson)
                .IsRequired()
                .HasMaxLength(500);
        });

        modelBuilder.Entity<RoundEntity>(e =>
        {
            e.HasKey(r => r.RoundId);

            e.HasIndex(r => new { r.SessionId, r.RoundNo })
                .IsUnique();

            e.Property(r => r.State)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>();

            e.Property(r => r.PreviewUrl)
                .IsRequired()
                .HasMaxLength(500);

            e.Property(r => r.AnswerTitle)
                .IsRequired()
                .HasMaxLength(300);

            e.Property(r => r.AnswerNorm)
                .IsRequired()
                .HasMaxLength(300);

            e.Property(r => r.AnswerArtistNorm)
                .IsRequired()
                .HasMaxLength(300);

            e.Property(r => r.AnswerArtist)
                .IsRequired()
                .HasMaxLength(300);

            e.Property(r => r.ArtworkUrl)
                .HasMaxLength(500);

            e.ToTable(t => t.HasCheckConstraint("CK_Round_RoundNo", "\"RoundNo\" >= 1"));
        });

        modelBuilder.Entity<RoundCorrectAnswerEntity>(e =>
        {
            e.HasKey(rca => new { rca.RoundId, rca.PlayerId });

            e.ToTable(t => t.HasCheckConstraint("CK_RoundCorrectAnswer_Points", "\"Points\" >= 0"));
        });
    }
}
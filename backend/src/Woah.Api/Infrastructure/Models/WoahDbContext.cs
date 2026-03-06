using Microsoft.EntityFrameworkCore;

namespace Woah.Api.Infrastructure.Models;

public class WoahDbContext : DbContext
{
    public WoahDbContext(DbContextOptions<WoahDbContext> options) : base(options) { }

    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<LobbyEntity> Lobbies => Set<LobbyEntity>();
    public DbSet<LobbyPlayerEntity> LobbyPlayers => Set<LobbyPlayerEntity>();
    public DbSet<PlaylistEntity> Playlists => Set<PlaylistEntity>();
    public DbSet<PlaylistTrackEntity> PlaylistTracks => Set<PlaylistTrackEntity>();
    public DbSet<GameSessionEntity> GameSessions => Set<GameSessionEntity>();
    public DbSet<RoundEntity> Rounds => Set<RoundEntity>();
    public DbSet<RoundCorrectAnswerEntity> RoundCorrectAnswers => Set<RoundCorrectAnswerEntity>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<PlayerEntity>()
        .HasKey(p => p.PlayerId);

    modelBuilder.Entity<LobbyEntity>()
        .HasKey(l => l.LobbyId);

    modelBuilder.Entity<LobbyEntity>()
        .HasOne(l => l.HostPlayer)
        .WithMany()
        .HasForeignKey(l => l.HostPlayerId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<LobbyPlayerEntity>()
        .HasKey(lp => new { lp.LobbyId, lp.PlayerId });

    modelBuilder.Entity<LobbyPlayerEntity>()
        .HasIndex(lp => new { lp.LobbyId, lp.Nick })
        .IsUnique();

    modelBuilder.Entity<LobbyPlayerEntity>()
        .HasOne(lp => lp.Lobby)
        .WithMany(l => l.LobbyPlayers)
        .HasForeignKey(lp => lp.LobbyId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<LobbyPlayerEntity>()
        .HasOne(lp => lp.Player)
        .WithMany(p => p.LobbyMemberships)
        .HasForeignKey(lp => lp.PlayerId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<PlaylistEntity>()
        .HasKey(pl => pl.PlaylistId);

    modelBuilder.Entity<PlaylistTrackEntity>()
        .HasKey(pt => new { pt.PlaylistId, pt.ItemNumber });

    modelBuilder.Entity<GameSessionEntity>()
        .HasKey(gs => gs.SessionId);

    modelBuilder.Entity<RoundEntity>()
        .HasKey(r => r.RoundId);

    modelBuilder.Entity<RoundEntity>()
        .HasIndex(r => new { r.SessionId, r.RoundNo })
        .IsUnique();

    modelBuilder.Entity<RoundCorrectAnswerEntity>()
        .HasKey(rca => new { rca.RoundId, rca.PlayerId });
}
}
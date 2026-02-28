using Microsoft.EntityFrameworkCore;
using Woah.Domain.Entities;

namespace Woah.Infrastructure.Persistence;

public sealed class WoahDbContext : DbContext
{
    public WoahDbContext(DbContextOptions<WoahDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Lobby> Lobbies => Set<Lobby>();
    public DbSet<LobbyPlayer> LobbyPlayers => Set<LobbyPlayer>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<RoundCorrectAnswer> RoundCorrectAnswers => Set<RoundCorrectAnswer>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyConcurrencyVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyConcurrencyVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WoahDbContext).Assembly);
    }

    private void ApplyConcurrencyVersions()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Metadata.FindProperty("Version") is null)
                continue;

            if (entry.State == EntityState.Added)
            {
                entry.Property("Version").CurrentValue = 0L;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                var original = entry.Property("Version").OriginalValue;
                var originalLong = original is null ? 0L : (long)original;
                entry.Property("Version").CurrentValue = originalLong + 1L;
            }
        }
    }
}
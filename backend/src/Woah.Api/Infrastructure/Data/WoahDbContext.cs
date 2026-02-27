using Microsoft.EntityFrameworkCore;
using Woah.Api.Infrastructure.Data.Entities;

namespace Woah.Api.Infrastructure.Data;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lobby>(b =>
        {
            b.ToTable("lobbies", tb =>
            {
                tb.HasCheckConstraint("lobbies_status_chk", "status IN ('waiting','playing','finished')");
                tb.HasCheckConstraint("lobbies_max_players_chk", "max_players BETWEEN 1 AND 10");
            });

            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("lobby_id");
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(16);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(16);
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.HostPlayerId).HasColumnName("host_player_id");
            b.Property(x => x.MaxPlayers).HasColumnName("max_players");

            b.HasIndex(x => x.Code).IsUnique();

            b.HasOne(x => x.HostPlayer)
                .WithMany()
                .HasForeignKey(x => x.HostPlayerId);
        });

        modelBuilder.Entity<LobbyPlayer>(b =>
        {
            b.ToTable("lobby_players");
            b.HasKey(x => new { x.LobbyId, x.PlayerId });

            b.Property(x => x.LobbyId).HasColumnName("lobby_id");
            b.Property(x => x.PlayerId).HasColumnName("player_id");
            b.Property(x => x.Nick).HasColumnName("nick").HasMaxLength(64);
            b.Property(x => x.JoinedAt).HasColumnName("joined_at");
            b.Property(x => x.LeftAt).HasColumnName("left_at");

            b.HasOne(x => x.Lobby)
                .WithMany(l => l.Players)
                .HasForeignKey(x => x.LobbyId);

            b.HasOne(x => x.Player)
                .WithMany()
                .HasForeignKey(x => x.PlayerId);

            b.HasIndex(x => new { x.LobbyId, x.Nick }).IsUnique();
        });

        modelBuilder.Entity<Playlist>(b =>
        {
            b.ToTable("playlists");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("playlist_id");
            b.Property(x => x.OwnerPlayerId).HasColumnName("owner_player_id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128);
            b.Property(x => x.Market).HasColumnName("market").HasMaxLength(8);
            b.Property(x => x.CreatedAt).HasColumnName("created_at");

            b.HasOne(x => x.OwnerPlayer)
                .WithMany()
                .HasForeignKey(x => x.OwnerPlayerId);
        });

        modelBuilder.Entity<PlaylistTrack>(b =>
        {
            b.ToTable("playlist_tracks");
            b.HasKey(x => new { x.PlaylistId, x.ItemNo });

            b.Property(x => x.PlaylistId).HasColumnName("playlist_id");
            b.Property(x => x.ItemNo).HasColumnName("item_no");
            b.Property(x => x.TrackJson).HasColumnName("track_json").HasColumnType("jsonb");
            b.Property(x => x.Title).HasColumnName("title");
            b.Property(x => x.PreviewUrl).HasColumnName("preview_url");
            b.Property(x => x.SpotifyTrackId).HasColumnName("spotify_track_id").HasMaxLength(64);
            b.Property(x => x.SpotifyUrl).HasColumnName("spotify_url");
            b.Property(x => x.IsValid).HasColumnName("is_valid");
            b.Property(x => x.InvalidReason).HasColumnName("invalid_reason");
            b.Property(x => x.CreatedAt).HasColumnName("created_at");

            b.HasOne(x => x.Playlist)
                .WithMany(p => p.Tracks)
                .HasForeignKey(x => x.PlaylistId);

            b.HasIndex(x => x.PlaylistId);

            b.HasIndex(x => new { x.PlaylistId, x.SpotifyTrackId })
                .IsUnique()
                .HasFilter("spotify_track_id IS NOT NULL");
        });

        modelBuilder.Entity<GameSession>(b =>
        {
            b.ToTable("game_sessions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("session_id");
            b.Property(x => x.LobbyId).HasColumnName("lobby_id");
            b.Property(x => x.PlaylistId).HasColumnName("playlist_id");
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.EndedAt).HasColumnName("ended_at");
            b.Property(x => x.SettingsJson).HasColumnName("settings_json").HasColumnType("jsonb");

            b.HasOne(x => x.Lobby)
                .WithMany()
                .HasForeignKey(x => x.LobbyId);

            b.HasOne(x => x.Playlist)
                .WithMany()
                .HasForeignKey(x => x.PlaylistId);
        });

        modelBuilder.Entity<Round>(b =>
        {
            b.ToTable("rounds", tb =>
            {
                tb.HasCheckConstraint("rounds_state_chk", "state IN ('running','revealed','finished')");
            });

            b.HasKey(x => x.Id);

            b.Property(x => x.Id).HasColumnName("round_id");
            b.Property(x => x.SessionId).HasColumnName("session_id");
            b.Property(x => x.RoundNo).HasColumnName("round_no");
            b.Property(x => x.PlaylistId).HasColumnName("playlist_id");
            b.Property(x => x.PlaylistItemNo).HasColumnName("playlist_item_no");
            b.Property(x => x.PreviewUrl).HasColumnName("preview_url");
            b.Property(x => x.AnswerTitle).HasColumnName("answer_title");
            b.Property(x => x.AnswerNorm).HasColumnName("answer_norm");
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.EndsAt).HasColumnName("ends_at");
            b.Property(x => x.RevealedAt).HasColumnName("revealed_at");
            b.Property(x => x.State).HasColumnName("state").HasMaxLength(16);

            b.HasOne(x => x.Session)
                .WithMany(s => s.Rounds)
                .HasForeignKey(x => x.SessionId);

            b.HasOne(x => x.PlaylistTrack)
                .WithMany()
                .HasForeignKey(x => new { x.PlaylistId, x.PlaylistItemNo });

            b.HasIndex(x => new { x.SessionId, x.RoundNo }).IsUnique();
        });

        modelBuilder.Entity<RoundCorrectAnswer>(b =>
        {
            b.ToTable("round_correct_answers");
            b.HasKey(x => new { x.RoundId, x.PlayerId });

            b.Property(x => x.RoundId).HasColumnName("round_id");
            b.Property(x => x.PlayerId).HasColumnName("player_id");
            b.Property(x => x.AnsweredAt).HasColumnName("answered_at");
            b.Property(x => x.Points).HasColumnName("points");

            b.HasOne(x => x.Round)
                .WithMany(r => r.CorrectAnswers)
                .HasForeignKey(x => x.RoundId);

            b.HasOne(x => x.Player)
                .WithMany()
                .HasForeignKey(x => x.PlayerId);

            b.HasIndex(x => x.RoundId);
            b.HasIndex(x => x.PlayerId);
        });
    }
}
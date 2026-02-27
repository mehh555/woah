using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woah.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lobbies",
                columns: table => new
                {
                    lobby_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    host_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_players = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lobbies", x => x.lobby_id);
                    table.CheckConstraint("lobbies_max_players_chk", "max_players BETWEEN 1 AND 10");
                    table.CheckConstraint("lobbies_status_chk", "status IN ('waiting','playing','finished')");
                    table.ForeignKey(
                        name: "FK_lobbies_Players_host_player_id",
                        column: x => x.host_player_id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlists",
                columns: table => new
                {
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    market = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlists", x => x.playlist_id);
                    table.ForeignKey(
                        name: "FK_playlists_Players_owner_player_id",
                        column: x => x.owner_player_id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lobby_players",
                columns: table => new
                {
                    lobby_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nick = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lobby_players", x => new { x.lobby_id, x.player_id });
                    table.ForeignKey(
                        name: "FK_lobby_players_Players_player_id",
                        column: x => x.player_id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lobby_players_lobbies_lobby_id",
                        column: x => x.lobby_id,
                        principalTable: "lobbies",
                        principalColumn: "lobby_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lobby_id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    settings_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_sessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_game_sessions_lobbies_lobby_id",
                        column: x => x.lobby_id,
                        principalTable: "lobbies",
                        principalColumn: "lobby_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_sessions_playlists_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "playlists",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_tracks",
                columns: table => new
                {
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_no = table.Column<int>(type: "integer", nullable: false),
                    track_json = table.Column<string>(type: "jsonb", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    preview_url = table.Column<string>(type: "text", nullable: true),
                    spotify_track_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    spotify_url = table.Column<string>(type: "text", nullable: true),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false),
                    invalid_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_tracks", x => new { x.playlist_id, x.item_no });
                    table.ForeignKey(
                        name: "FK_playlist_tracks_playlists_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "playlists",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rounds",
                columns: table => new
                {
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_no = table.Column<int>(type: "integer", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_item_no = table.Column<int>(type: "integer", nullable: false),
                    preview_url = table.Column<string>(type: "text", nullable: false),
                    answer_title = table.Column<string>(type: "text", nullable: false),
                    answer_norm = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revealed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rounds", x => x.round_id);
                    table.CheckConstraint("rounds_state_chk", "state IN ('running','revealed','finished')");
                    table.ForeignKey(
                        name: "FK_rounds_game_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "game_sessions",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rounds_playlist_tracks_playlist_id_playlist_item_no",
                        columns: x => new { x.playlist_id, x.playlist_item_no },
                        principalTable: "playlist_tracks",
                        principalColumns: new[] { "playlist_id", "item_no" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "round_correct_answers",
                columns: table => new
                {
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_round_correct_answers", x => new { x.round_id, x.player_id });
                    table.ForeignKey(
                        name: "FK_round_correct_answers_Players_player_id",
                        column: x => x.player_id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_round_correct_answers_rounds_round_id",
                        column: x => x.round_id,
                        principalTable: "rounds",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_lobby_id",
                table: "game_sessions",
                column: "lobby_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_playlist_id",
                table: "game_sessions",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_lobbies_code",
                table: "lobbies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lobbies_host_player_id",
                table: "lobbies",
                column: "host_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_lobby_players_lobby_id_nick",
                table: "lobby_players",
                columns: new[] { "lobby_id", "nick" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lobby_players_player_id",
                table: "lobby_players",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_tracks_playlist_id",
                table: "playlist_tracks",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_tracks_playlist_id_spotify_track_id",
                table: "playlist_tracks",
                columns: new[] { "playlist_id", "spotify_track_id" },
                unique: true,
                filter: "spotify_track_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_playlists_owner_player_id",
                table: "playlists",
                column: "owner_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_round_correct_answers_player_id",
                table: "round_correct_answers",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_round_correct_answers_round_id",
                table: "round_correct_answers",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_rounds_playlist_id_playlist_item_no",
                table: "rounds",
                columns: new[] { "playlist_id", "playlist_item_no" });

            migrationBuilder.CreateIndex(
                name: "IX_rounds_session_id_round_no",
                table: "rounds",
                columns: new[] { "session_id", "round_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lobby_players");

            migrationBuilder.DropTable(
                name: "round_correct_answers");

            migrationBuilder.DropTable(
                name: "rounds");

            migrationBuilder.DropTable(
                name: "game_sessions");

            migrationBuilder.DropTable(
                name: "playlist_tracks");

            migrationBuilder.DropTable(
                name: "lobbies");

            migrationBuilder.DropTable(
                name: "playlists");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}

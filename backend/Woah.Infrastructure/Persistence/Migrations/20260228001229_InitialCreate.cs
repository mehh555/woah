using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                name: "Lobbies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HostPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxPlayers = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lobbies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lobbies_Players_HostPlayerId",
                        column: x => x.HostPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    OwnerPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Market = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playlists_Players_OwnerPlayerId",
                        column: x => x.OwnerPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LobbyPlayers",
                columns: table => new
                {
                    LobbyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nick = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyPlayers", x => new { x.LobbyId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_LobbyPlayers_Lobbies_LobbyId",
                        column: x => x.LobbyId,
                        principalTable: "Lobbies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LobbyPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LobbyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_Lobbies_LobbyId",
                        column: x => x.LobbyId,
                        principalTable: "Lobbies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameSessions_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistTracks",
                columns: table => new
                {
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNo = table.Column<int>(type: "integer", nullable: false),
                    TrackJson = table.Column<string>(type: "jsonb", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PreviewUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SpotifyTrackId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SpotifyUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    InvalidReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistTracks", x => new { x.PlaylistId, x.ItemNo });
                    table.ForeignKey(
                        name: "FK_PlaylistTracks_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNo = table.Column<int>(type: "integer", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaylistItemNo = table.Column<int>(type: "integer", nullable: false),
                    PreviewUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AnswerTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AnswerNorm = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevealedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rounds_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rounds_PlaylistTracks_PlaylistId_PlaylistItemNo",
                        columns: x => new { x.PlaylistId, x.PlaylistItemNo },
                        principalTable: "PlaylistTracks",
                        principalColumns: new[] { "PlaylistId", "ItemNo" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoundCorrectAnswers",
                columns: table => new
                {
                    RoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundCorrectAnswers", x => new { x.RoundId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_RoundCorrectAnswers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundCorrectAnswers_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_LobbyId",
                table: "GameSessions",
                column: "LobbyId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_PlaylistId",
                table: "GameSessions",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_Code",
                table: "Lobbies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_HostPlayerId",
                table: "Lobbies",
                column: "HostPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyPlayers_LobbyId_Nick",
                table: "LobbyPlayers",
                columns: new[] { "LobbyId", "Nick" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LobbyPlayers_PlayerId",
                table: "LobbyPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_OwnerPlayerId",
                table: "Playlists",
                column: "OwnerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_PlaylistId_SpotifyTrackId",
                table: "PlaylistTracks",
                columns: new[] { "PlaylistId", "SpotifyTrackId" },
                unique: true,
                filter: "spotify_track_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoundCorrectAnswers_PlayerId",
                table: "RoundCorrectAnswers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_PlaylistId_PlaylistItemNo",
                table: "Rounds",
                columns: new[] { "PlaylistId", "PlaylistItemNo" });

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_SessionId_RoundNo",
                table: "Rounds",
                columns: new[] { "SessionId", "RoundNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LobbyPlayers");

            migrationBuilder.DropTable(
                name: "RoundCorrectAnswers");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "PlaylistTracks");

            migrationBuilder.DropTable(
                name: "Lobbies");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}

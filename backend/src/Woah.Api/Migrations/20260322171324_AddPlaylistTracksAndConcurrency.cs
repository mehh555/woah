using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woah.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistTracksAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "RoundCorrectAnswers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "ActivePlaylistId",
                table: "Lobbies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PlaylistTracks",
                columns: table => new
                {
                    PlaylistTrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItunesTrackId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Artist = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PreviewUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ArtworkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistTracks", x => x.PlaylistTrackId);
                    table.ForeignKey(
                        name: "FK_PlaylistTracks_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "PlaylistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_ActivePlaylistId",
                table: "Lobbies",
                column: "ActivePlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_PlaylistId_ItunesTrackId",
                table: "PlaylistTracks",
                columns: new[] { "PlaylistId", "ItunesTrackId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lobbies_Playlists_ActivePlaylistId",
                table: "Lobbies",
                column: "ActivePlaylistId",
                principalTable: "Playlists",
                principalColumn: "PlaylistId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lobbies_Playlists_ActivePlaylistId",
                table: "Lobbies");

            migrationBuilder.DropTable(
                name: "PlaylistTracks");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_ActivePlaylistId",
                table: "Lobbies");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "RoundCorrectAnswers");

            migrationBuilder.DropColumn(
                name: "ActivePlaylistId",
                table: "Lobbies");
        }
    }
}

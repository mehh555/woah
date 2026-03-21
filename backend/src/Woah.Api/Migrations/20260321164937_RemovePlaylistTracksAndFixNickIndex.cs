using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woah.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlaylistTracksAndFixNickIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaylistTracks");

            migrationBuilder.DropIndex(
                name: "IX_LobbyPlayers_LobbyId_Nick",
                table: "LobbyPlayers");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyPlayers_LobbyId_Nick",
                table: "LobbyPlayers",
                columns: new[] { "LobbyId", "Nick" },
                unique: true,
                filter: "\"LeftAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_Code",
                table: "Lobbies",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LobbyPlayers_LobbyId_Nick",
                table: "LobbyPlayers");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_Code",
                table: "Lobbies");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlaylistTracks",
                columns: table => new
                {
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistTracks", x => new { x.PlaylistId, x.ItemNumber });
                    table.ForeignKey(
                        name: "FK_PlaylistTracks_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "PlaylistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LobbyPlayers_LobbyId_Nick",
                table: "LobbyPlayers",
                columns: new[] { "LobbyId", "Nick" },
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woah.Api.Migrations
{
    /// <inheritdoc />
    public partial class PlayerPlaylistAndFeatCleaner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddedByPlayerId",
                table: "PlaylistTracks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_AddedByPlayerId",
                table: "PlaylistTracks",
                column: "AddedByPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistTracks_Players_AddedByPlayerId",
                table: "PlaylistTracks",
                column: "AddedByPlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistTracks_Players_AddedByPlayerId",
                table: "PlaylistTracks");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistTracks_AddedByPlayerId",
                table: "PlaylistTracks");

            migrationBuilder.DropColumn(
                name: "AddedByPlayerId",
                table: "PlaylistTracks");
        }
    }
}

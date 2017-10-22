using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordPugBot.Migrations
{
    public partial class GameModevoting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameModeId",
                table: "Matches",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_GameModeId",
                table: "Matches",
                column: "GameModeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_GameModes_GameModeId",
                table: "Matches",
                column: "GameModeId",
                principalTable: "GameModes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_GameModes_GameModeId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_GameModeId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GameModeId",
                table: "Matches");
        }
    }
}

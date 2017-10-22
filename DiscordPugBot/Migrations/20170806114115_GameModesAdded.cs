using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordPugBot.Migrations
{
    public partial class GameModesAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BattleTag",
                table: "Users",
                newName: "SteamId");

            migrationBuilder.DropColumn(
                name: "MapType",
                table: "Maps");

            migrationBuilder.CreateTable(
                name: "GameModes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameModes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SteamId",
                table: "Users",
                newName: "BattleTag");

            migrationBuilder.DropTable(
                name: "GameModes");

            migrationBuilder.AddColumn<int>(
                name: "MapType",
                table: "Maps",
                type: "int(11)",
                nullable: false,
                defaultValueSql: "0")
                .Annotation("MySql:ValueGeneratedOnAdd", true);
        }
    }
}

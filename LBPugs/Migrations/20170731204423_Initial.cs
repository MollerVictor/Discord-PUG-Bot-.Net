using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LBPugs.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    MapType = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    BattleTag = table.Column<string>(nullable: true),
                    DiscordId = table.Column<long>(type: "bigint(20)", nullable: true),
                    FatKided = table.Column<int>(nullable: false),
                    Info = table.Column<string>(type: "varchar(200)", nullable: true),
                    Loses = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    LosesAsCaptain = table.Column<int>(nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    PlayerHeroes = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    SkillRating = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "5000")
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    UserName = table.Column<string>(type: "varchar(128)", nullable: true),
                    Wins = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    WinsAsCaptain = table.Column<int>(nullable: false, defaultValueSql: "0")
                        .Annotation("MySql:ValueGeneratedOnAdd", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGeneratedOnAdd", true),
                    MapId = table.Column<int>(nullable: true),
                    MatchState = table.Column<int>(nullable: false),
                    PlayedDate = table.Column<DateTime>(nullable: false),
                    TeamWinner = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsersMatchesRelation",
                columns: table => new
                {
                    MatchId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    IsCaptain = table.Column<bool>(nullable: false),
                    SkillRating = table.Column<int>(nullable: false),
                    Team = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersMatchesRelation", x => new { x.MatchId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UsersMatchesRelation_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsersMatchesRelation_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MapId",
                table: "Matches",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersMatchesRelation_UserId",
                table: "UsersMatchesRelation",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsersMatchesRelation");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordPugBot.Migrations
{
    public partial class glicko : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RatingsDeviation",
                table: "Users",
                nullable: false,
                defaultValueSql: "350")
                .Annotation("MySql:ValueGeneratedOnAdd", true);

            migrationBuilder.AddColumn<double>(
                name: "Volatility",
                table: "Users",
                nullable: false,
                defaultValueSql: "0.06")
                .Annotation("MySql:ValueGeneratedOnAdd", true);

            migrationBuilder.AlterColumn<double>(
                name: "SkillRating",
                table: "UsersMatchesRelation",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<double>(
                name: "SkillRating",
                table: "Users",
                nullable: false,
                defaultValueSql: "1500",
                oldClrType: typeof(int),
                oldType: "int(11)",
                oldDefaultValueSql: "2000")
                .Annotation("MySql:ValueGeneratedOnAdd", true)
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RatingsDeviation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Volatility",
                table: "Users");

            migrationBuilder.AlterColumn<int>(
                name: "SkillRating",
                table: "UsersMatchesRelation",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AlterColumn<int>(
                name: "SkillRating",
                table: "Users",
                type: "int(11)",
                nullable: false,
                defaultValueSql: "2000",
                oldClrType: typeof(double),
                oldDefaultValueSql: "1500")
                .Annotation("MySql:ValueGeneratedOnAdd", true)
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }
    }
}

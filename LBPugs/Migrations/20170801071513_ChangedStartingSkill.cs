using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LBPugs.Migrations
{
    public partial class ChangedStartingSkill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SkillRating",
                table: "Users",
                type: "int(11)",
                nullable: false,
                defaultValueSql: "2000",
                oldClrType: typeof(int),
                oldType: "int(11)",
                oldDefaultValueSql: "5000")
                .Annotation("MySql:ValueGeneratedOnAdd", true)
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SkillRating",
                table: "Users",
                type: "int(11)",
                nullable: false,
                defaultValueSql: "5000",
                oldClrType: typeof(int),
                oldType: "int(11)",
                oldDefaultValueSql: "2000")
                .Annotation("MySql:ValueGeneratedOnAdd", true)
                .OldAnnotation("MySql:ValueGeneratedOnAdd", true);
        }
    }
}

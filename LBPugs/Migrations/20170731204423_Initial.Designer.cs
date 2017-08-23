using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using OWPugs.Models;

namespace LBPugs.Migrations
{
    [DbContext(typeof(MyDBContext))]
    [Migration("20170731204423_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("OWPugs.Models.Maps", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)");

                    b.Property<int>("MapType")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)")
                        .HasDefaultValueSql("0");

                    b.Property<string>("Name")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("varchar(255)")
                        .HasDefaultValueSql("0");

                    b.HasKey("Id");

                    b.ToTable("Maps");
                });

            modelBuilder.Entity("OWPugs.Models.Matches", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("MapId");

                    b.Property<int>("MatchState");

                    b.Property<DateTime>("PlayedDate");

                    b.Property<int>("TeamWinner");

                    b.HasKey("Id");

                    b.HasIndex("MapId");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("OWPugs.Models.Users", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)");

                    b.Property<string>("BattleTag");

                    b.Property<long?>("DiscordId")
                        .HasColumnType("bigint(20)");

                    b.Property<int>("FatKided");

                    b.Property<string>("Info")
                        .HasColumnType("varchar(200)");

                    b.Property<int>("Loses")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)")
                        .HasDefaultValueSql("0");

                    b.Property<int>("LosesAsCaptain")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.Property<int>("PlayerHeroes")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)")
                        .HasDefaultValueSql("0");

                    b.Property<int>("SkillRating")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)")
                        .HasDefaultValueSql("5000");

                    b.Property<string>("UserName")
                        .HasColumnType("varchar(128)");

                    b.Property<int>("Wins")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(11)")
                        .HasDefaultValueSql("0");

                    b.Property<int>("WinsAsCaptain")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("0");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("OWPugs.Models.UsersMatchesRelation", b =>
                {
                    b.Property<int>("MatchId");

                    b.Property<int>("UserId");

                    b.Property<bool>("IsCaptain");

                    b.Property<int>("SkillRating");

                    b.Property<int>("Team");

                    b.HasKey("MatchId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("UsersMatchesRelation");
                });

            modelBuilder.Entity("OWPugs.Models.Matches", b =>
                {
                    b.HasOne("OWPugs.Models.Maps", "Map")
                        .WithMany()
                        .HasForeignKey("MapId");
                });

            modelBuilder.Entity("OWPugs.Models.UsersMatchesRelation", b =>
                {
                    b.HasOne("OWPugs.Models.Matches", "Match")
                        .WithMany("UserMatches")
                        .HasForeignKey("MatchId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OWPugs.Models.Users", "User")
                        .WithMany("UserMatches")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}

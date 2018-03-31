using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace WamBotRewrite.Migrations
{
    public partial class Info : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Guilds",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Connections",
                table: "Channels",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MessagesSent",
                table: "Channels",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Channels",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Channels",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "Connections",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "MessagesSent",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Channels");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_token",
                schema: "public",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_token_expires_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_expires_at",
                schema: "public",
                table: "users");
        }
    }
}

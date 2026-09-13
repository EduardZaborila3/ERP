using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpServer.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsDeletedWithDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "public",
                table: "users");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "public",
                table: "users");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

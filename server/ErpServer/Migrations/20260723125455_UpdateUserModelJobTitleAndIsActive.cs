using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserModelJobTitleAndIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_approved",
                schema: "public",
                table: "users",
                newName: "is_active");

            migrationBuilder.AddColumn<int>(
                name: "job_title_id",
                schema: "public",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "job_title_id",
                schema: "public",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "public",
                table: "users",
                newName: "is_approved");
        }
    }
}

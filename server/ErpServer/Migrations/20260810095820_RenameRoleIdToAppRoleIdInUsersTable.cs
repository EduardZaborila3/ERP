using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpServer.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoleIdToAppRoleIdInUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "role_id",
                schema: "public",
                table: "users",
                newName: "app_role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "app_role_id",
                schema: "public",
                table: "users",
                newName: "role_id");
        }
    }
}

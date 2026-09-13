using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpServer.Migrations
{
    /// <inheritdoc />
    public partial class FixAppRoleTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Table app_roles already exists in database public schema.
            // This migration aligns EF Core model snapshot with the actual database table name.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

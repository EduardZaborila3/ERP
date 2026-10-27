using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpServer.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibleUserToCars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "responsible_user_id",
                table: "cars",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_cars_responsible_user_id",
                table: "cars",
                column: "responsible_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cars_users_responsible_user_id",
                table: "cars",
                column: "responsible_user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cars_users_responsible_user_id",
                table: "cars");

            migrationBuilder.DropIndex(
                name: "ix_cars_responsible_user_id",
                table: "cars");

            migrationBuilder.DropColumn(
                name: "responsible_user_id",
                table: "cars");
        }
    }
}

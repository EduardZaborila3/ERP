using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverToStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "driver_id",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver_name",
                table: "stock_movements",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_driver_id",
                table: "stock_movements",
                column: "driver_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_users_driver_id",
                table: "stock_movements",
                column: "driver_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_users_driver_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_driver_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "driver_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "driver_name",
                table: "stock_movements");
        }
    }
}

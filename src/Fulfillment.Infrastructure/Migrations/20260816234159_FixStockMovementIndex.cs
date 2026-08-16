using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fulfillment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixStockMovementIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_InventoryItemId_OccuredAt",
                table: "StockMovements");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryItemId_OccuredAt",
                table: "StockMovements",
                columns: new[] { "InventoryItemId", "OccuredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_InventoryItemId_OccuredAt",
                table: "StockMovements");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryItemId_OccuredAt",
                table: "StockMovements",
                columns: new[] { "InventoryItemId", "OccuredAt" },
                unique: true);
        }
    }
}

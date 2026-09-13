using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopOrderPrintAssetUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PrintReadyQrAssets_ShopOrderId_QrType",
                table: "PrintReadyQrAssets",
                columns: new[] { "ShopOrderId", "QrType" },
                unique: true,
                filter: "[ShopOrderId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrintReadyQrAssets_ShopOrderId_QrType",
                table: "PrintReadyQrAssets");
        }
    }
}

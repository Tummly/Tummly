using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class UniqueActiveShopMaterialQrType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkus_QrType",
                table: "ShopMaterialSkus",
                column: "QrType",
                unique: true,
                filter: "[Status] <> N'Deleted'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopMaterialSkus_QrType",
                table: "ShopMaterialSkus");
        }
    }
}

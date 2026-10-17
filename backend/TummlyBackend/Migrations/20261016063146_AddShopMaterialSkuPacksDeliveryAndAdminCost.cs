using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopMaterialSkuPacksDeliveryAndAdminCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryNetPence",
                table: "ShopMaterialSkus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InternalCostNetPence",
                table: "ShopMaterialSkus",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PacksJson",
                table: "ShopMaterialSkus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "ShopMaterialSkus",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryNetPence",
                table: "ShopMaterialSkus");

            migrationBuilder.DropColumn(
                name: "InternalCostNetPence",
                table: "ShopMaterialSkus");

            migrationBuilder.DropColumn(
                name: "PacksJson",
                table: "ShopMaterialSkus");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "ShopMaterialSkus");
        }
    }
}

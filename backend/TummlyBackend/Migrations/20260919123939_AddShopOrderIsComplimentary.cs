using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopOrderIsComplimentary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsComplimentary",
                table: "ShopOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_LocationId_Complimentary",
                table: "ShopOrders",
                column: "LocationId",
                unique: true,
                filter: "[IsComplimentary] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopOrders_LocationId_Complimentary",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "IsComplimentary",
                table: "ShopOrders");
        }
    }
}

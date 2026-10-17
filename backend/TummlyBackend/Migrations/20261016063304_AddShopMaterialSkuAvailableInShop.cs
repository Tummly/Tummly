using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopMaterialSkuAvailableInShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AvailableInShop",
                table: "ShopMaterialSkus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Keep existing Published SKUs orderable until Admin toggles them OFF.
            migrationBuilder.Sql(
                """
                UPDATE [ShopMaterialSkus]
                SET [AvailableInShop] = CAST(1 AS bit)
                WHERE [Status] = N'Published';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableInShop",
                table: "ShopMaterialSkus");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class ComplimentaryStarterByRecipeAndSkuIdReuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopMaterialSkus_SkuId",
                table: "ShopMaterialSkus");

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInComplimentaryStarter",
                table: "PlacementRecipes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE [PlacementRecipes]
                SET [IncludeInComplimentaryStarter] = CAST(1 AS bit)
                WHERE [Slug] IN (N'table-tent', N'window-sticker', N'offer-card');
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkus_SkuId",
                table: "ShopMaterialSkus",
                column: "SkuId",
                unique: true,
                filter: "[Status] <> N'Deleted'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopMaterialSkus_SkuId",
                table: "ShopMaterialSkus");

            migrationBuilder.DropColumn(
                name: "IncludeInComplimentaryStarter",
                table: "PlacementRecipes");

            migrationBuilder.CreateIndex(
                name: "IX_ShopMaterialSkus_SkuId",
                table: "ShopMaterialSkus",
                column: "SkuId",
                unique: true);
        }
    }
}

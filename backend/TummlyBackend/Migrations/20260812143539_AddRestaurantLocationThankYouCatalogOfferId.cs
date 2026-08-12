using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantLocationThankYouCatalogOfferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThankYouCatalogOfferId",
                table: "RestaurantLocations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_ThankYouCatalogOfferId",
                table: "RestaurantLocations",
                column: "ThankYouCatalogOfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_CatalogOffers_ThankYouCatalogOfferId",
                table: "RestaurantLocations",
                column: "ThankYouCatalogOfferId",
                principalTable: "CatalogOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_CatalogOffers_ThankYouCatalogOfferId",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_ThankYouCatalogOfferId",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "ThankYouCatalogOfferId",
                table: "RestaurantLocations");
        }
    }
}

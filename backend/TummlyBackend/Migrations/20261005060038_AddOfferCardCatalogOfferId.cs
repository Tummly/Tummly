using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferCardCatalogOfferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfferCardCatalogOfferId",
                table: "RestaurantLocations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_OfferCardCatalogOfferId",
                table: "RestaurantLocations",
                column: "OfferCardCatalogOfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_CatalogOffers_OfferCardCatalogOfferId",
                table: "RestaurantLocations",
                column: "OfferCardCatalogOfferId",
                principalTable: "CatalogOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_CatalogOffers_OfferCardCatalogOfferId",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_OfferCardCatalogOfferId",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "OfferCardCatalogOfferId",
                table: "RestaurantLocations");
        }
    }
}

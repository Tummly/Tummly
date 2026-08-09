using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOffersCatalogAndCampaignOfferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfferId",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OfferType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Validity = table.Column<int>(type: "int", nullable: false),
                    CustomExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    FreeItemText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PurchaseRequirement = table.Column<int>(type: "int", nullable: true),
                    MinimumSpend = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    AdditionalExclusions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReplacementItemText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaffInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogOffers_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_OfferId",
                table: "Campaigns",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogOffers_RestaurantLocationId_Status",
                table: "CatalogOffers",
                columns: new[] { "RestaurantLocationId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_CatalogOffers_OfferId",
                table: "Campaigns",
                column: "OfferId",
                principalTable: "CatalogOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_CatalogOffers_OfferId",
                table: "Campaigns");

            migrationBuilder.DropTable(
                name: "CatalogOffers");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_OfferId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "Campaigns");
        }
    }
}

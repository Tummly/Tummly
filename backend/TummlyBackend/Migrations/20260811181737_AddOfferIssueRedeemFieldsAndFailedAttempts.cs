using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferIssueRedeemFieldsAndFailedAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "OfferIssues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RedeemedAtUtc",
                table: "OfferIssues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OfferRedeemFailedAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogOfferId = table.Column<int>(type: "int", nullable: false),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    AttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferRedeemFailedAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferRedeemFailedAttempts_CatalogOffers_CatalogOfferId",
                        column: x => x.CatalogOfferId,
                        principalTable: "CatalogOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferRedeemFailedAttempts_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferRedeemFailedAttempts_CatalogOfferId_AttemptedAtUtc",
                table: "OfferRedeemFailedAttempts",
                columns: new[] { "CatalogOfferId", "AttemptedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferRedeemFailedAttempts_RestaurantLocationId",
                table: "OfferRedeemFailedAttempts",
                column: "RestaurantLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferRedeemFailedAttempts");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "OfferIssues");

            migrationBuilder.DropColumn(
                name: "RedeemedAtUtc",
                table: "OfferIssues");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfferIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatalogOfferId = table.Column<int>(type: "int", nullable: false),
                    LocationGuestId = table.Column<int>(type: "int", nullable: false),
                    ClaimCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CampaignId = table.Column<int>(type: "int", nullable: true),
                    FeedbackId = table.Column<int>(type: "int", nullable: true),
                    ExpiryAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    StaffInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferIssues_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferIssues_CatalogOffers_CatalogOfferId",
                        column: x => x.CatalogOfferId,
                        principalTable: "CatalogOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferIssues_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferIssues_LocationGuests_LocationGuestId",
                        column: x => x.LocationGuestId,
                        principalTable: "LocationGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_CampaignId_LocationGuestId",
                table: "OfferIssues",
                columns: new[] { "CampaignId", "LocationGuestId" },
                unique: true,
                filter: "[CampaignId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_CatalogOfferId",
                table: "OfferIssues",
                column: "CatalogOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_ClaimCode",
                table: "OfferIssues",
                column: "ClaimCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_FeedbackId",
                table: "OfferIssues",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_LocationGuestId",
                table: "OfferIssues",
                column: "LocationGuestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferIssues");
        }
    }
}

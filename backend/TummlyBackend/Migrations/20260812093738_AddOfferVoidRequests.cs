using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferVoidRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RedemptionVoidedAtUtc",
                table: "OfferIssues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OfferVoidRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferIssueId = table.Column<int>(type: "int", nullable: false),
                    CatalogOfferId = table.Column<int>(type: "int", nullable: false),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalRedeemedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReasonId = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CorrectionId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferVoidRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferVoidRequests_CatalogOffers_CatalogOfferId",
                        column: x => x.CatalogOfferId,
                        principalTable: "CatalogOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferVoidRequests_OfferIssues_OfferIssueId",
                        column: x => x.OfferIssueId,
                        principalTable: "OfferIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferVoidRequests_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferVoidRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferVoidRequests_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferVoidRequests_CatalogOfferId",
                table: "OfferVoidRequests",
                column: "CatalogOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferVoidRequests_OfferIssueId_Status",
                table: "OfferVoidRequests",
                columns: new[] { "OfferIssueId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferVoidRequests_RequestedByUserId",
                table: "OfferVoidRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferVoidRequests_ResolvedByUserId",
                table: "OfferVoidRequests",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferVoidRequests_RestaurantLocationId",
                table: "OfferVoidRequests",
                column: "RestaurantLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferVoidRequests");

            migrationBuilder.DropColumn(
                name: "RedemptionVoidedAtUtc",
                table: "OfferIssues");
        }
    }
}

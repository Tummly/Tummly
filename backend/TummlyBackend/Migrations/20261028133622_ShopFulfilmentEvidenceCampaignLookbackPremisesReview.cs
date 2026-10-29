using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class ShopFulfilmentEvidenceCampaignLookbackPremisesReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliverySource",
                table: "ShopOrders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchCarrierName",
                table: "ShopOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchCarrierReference",
                table: "ShopOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchSource",
                table: "ShopOrders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DispatchedByAdminUserId",
                table: "ShopOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreparationStartedAtUtc",
                table: "ShopOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreparationStartedByAdminUserId",
                table: "ShopOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionStartSource",
                table: "ShopOrders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FsaEstablishmentId",
                table: "RestaurantLocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PremisesKey",
                table: "RestaurantLocations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PremisesReviewReason",
                table: "RestaurantLocations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PremisesReviewStatus",
                table: "RestaurantLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PremisesReviewedAtUtc",
                table: "RestaurantLocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PremisesReviewedByAdminUserId",
                table: "RestaurantLocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AudienceLookbackDays",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudienceLookbackTimeZone",
                table: "Campaigns",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationSourceKey",
                table: "Campaigns",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_FsaEstablishmentId",
                table: "RestaurantLocations",
                column: "FsaEstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_PremisesKey",
                table: "RestaurantLocations",
                column: "PremisesKey");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_PremisesReviewStatus",
                table: "RestaurantLocations",
                column: "PremisesReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_RestaurantLocationId_RecommendationSourceKey",
                table: "Campaigns",
                columns: new[] { "RestaurantLocationId", "RecommendationSourceKey" },
                unique: true,
                filter: "[Status] = N'draft' AND [RecommendationSourceKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_FsaEstablishmentId",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_PremisesKey",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_PremisesReviewStatus",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_RestaurantLocationId_RecommendationSourceKey",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "DeliverySource",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchCarrierName",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchCarrierReference",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchSource",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DispatchedByAdminUserId",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "PreparationStartedAtUtc",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "PreparationStartedByAdminUserId",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "ProductionStartSource",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "FsaEstablishmentId",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "PremisesKey",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "PremisesReviewReason",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "PremisesReviewStatus",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "PremisesReviewedAtUtc",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "PremisesReviewedByAdminUserId",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "AudienceLookbackDays",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "AudienceLookbackTimeZone",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "RecommendationSourceKey",
                table: "Campaigns");
        }
    }
}

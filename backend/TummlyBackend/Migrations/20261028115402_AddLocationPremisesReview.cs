using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationPremisesReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}

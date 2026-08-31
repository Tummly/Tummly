using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationLifecycleCityManagerPrivacyConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyConsentReadyAt",
                table: "Restaurants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "RestaurantLocations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifecycleStatus",
                table: "RestaurantLocations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ManagerUserId",
                table: "RestaurantLocations",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE RestaurantLocations SET LifecycleStatus = 1;
                UPDATE Restaurants SET PrivacyConsentReadyAt = GETUTCDATE() WHERE PrivacyConsentReadyAt IS NULL;
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLocations_ManagerUserId",
                table: "RestaurantLocations",
                column: "ManagerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantLocations_Users_ManagerUserId",
                table: "RestaurantLocations",
                column: "ManagerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantLocations_Users_ManagerUserId",
                table: "RestaurantLocations");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLocations_ManagerUserId",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "PrivacyConsentReadyAt",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "City",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "RestaurantLocations");
        }
    }
}

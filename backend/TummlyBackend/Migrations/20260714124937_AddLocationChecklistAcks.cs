using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationChecklistAcks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GuestFormPreviewedAt",
                table: "RestaurantLocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrPlacementGuideViewedAt",
                table: "RestaurantLocations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestFormPreviewedAt",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "QrPlacementGuideViewedAt",
                table: "RestaurantLocations");
        }
    }
}

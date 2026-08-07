using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Changes Campaigns → RestaurantLocations from Cascade to Restrict so
    /// hard-deleting a location that still has Campaigns fails with a FK
    /// constraint error instead of wiping Drafts (campaigns-audit/15).
    /// </remarks>
    public partial class RestrictCampaignLocationDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_RestaurantLocations_RestaurantLocationId",
                table: "Campaigns");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_RestaurantLocations_RestaurantLocationId",
                table: "Campaigns",
                column: "RestaurantLocationId",
                principalTable: "RestaurantLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_RestaurantLocations_RestaurantLocationId",
                table: "Campaigns");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_RestaurantLocations_RestaurantLocationId",
                table: "Campaigns",
                column: "RestaurantLocationId",
                principalTable: "RestaurantLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

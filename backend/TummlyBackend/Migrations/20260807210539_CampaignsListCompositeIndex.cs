using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class CampaignsListCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Campaigns_RestaurantLocationId_Status",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_RestaurantLocationId_UpdatedAt",
                table: "Campaigns");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_RestaurantLocationId_Status_UpdatedAt",
                table: "Campaigns",
                columns: new[] { "RestaurantLocationId", "Status", "UpdatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Campaigns_RestaurantLocationId_Status_UpdatedAt",
                table: "Campaigns");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_RestaurantLocationId_Status",
                table: "Campaigns",
                columns: new[] { "RestaurantLocationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_RestaurantLocationId_UpdatedAt",
                table: "Campaigns",
                columns: new[] { "RestaurantLocationId", "UpdatedAt" });
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignAudienceScopeConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CampaignAudiences_RestaurantLocationId_Name",
                table: "CampaignAudiences",
                columns: new[] { "RestaurantLocationId", "Name" },
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignAudiences_RestaurantLocations_RestaurantLocationId",
                table: "CampaignAudiences",
                column: "RestaurantLocationId",
                principalTable: "RestaurantLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CampaignAudiences_RestaurantLocations_RestaurantLocationId",
                table: "CampaignAudiences");

            migrationBuilder.DropIndex(
                name: "IX_CampaignAudiences_RestaurantLocationId_Name",
                table: "CampaignAudiences");
        }
    }
}

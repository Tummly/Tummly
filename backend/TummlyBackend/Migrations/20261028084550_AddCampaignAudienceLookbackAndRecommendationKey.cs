using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignAudienceLookbackAndRecommendationKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_Campaigns_RestaurantLocationId_RecommendationSourceKey",
                table: "Campaigns");

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

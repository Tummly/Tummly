using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantWorkspaceDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WeekKey",
                table: "WeeklyBriefs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCampaignSenderName",
                table: "Restaurants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultReportingPeriod",
                table: "Restaurants",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeekStartsOn",
                table: "Restaurants",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCampaignSenderName",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "DefaultReportingPeriod",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "WeekStartsOn",
                table: "Restaurants");

            migrationBuilder.AlterColumn<string>(
                name: "WeekKey",
                table: "WeeklyBriefs",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);
        }
    }
}

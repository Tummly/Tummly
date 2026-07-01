using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialRequestMainLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainLocation",
                table: "TrialRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "TrialRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TownCity",
                table: "TrialRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MainLocation",
                table: "PendingTrialRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "PendingTrialRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TownCity",
                table: "PendingTrialRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainLocation",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "TownCity",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "MainLocation",
                table: "PendingTrialRequests");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "PendingTrialRequests");

            migrationBuilder.DropColumn(
                name: "TownCity",
                table: "PendingTrialRequests");
        }
    }
}

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
            migrationBuilder.DropIndex(
                name: "IX_TrustedDevices_UserId",
                table: "TrustedDevices");

            migrationBuilder.AddColumn<bool>(
                name: "HasCompletedFirstSignIn",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SelectedLocationId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "TrustedDevices",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelectedLocationId",
                table: "Users",
                column: "SelectedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_UserId_TokenHash",
                table: "TrustedDevices",
                columns: new[] { "UserId", "TokenHash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_RestaurantLocations_SelectedLocationId",
                table: "Users",
                column: "SelectedLocationId",
                principalTable: "RestaurantLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_RestaurantLocations_SelectedLocationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedLocationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TrustedDevices_UserId_TokenHash",
                table: "TrustedDevices");

            migrationBuilder.DropColumn(
                name: "HasCompletedFirstSignIn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedLocationId",
                table: "Users");

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

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "TrustedDevices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_TrustedDevices_UserId",
                table: "TrustedDevices",
                column: "UserId");
        }
    }
}

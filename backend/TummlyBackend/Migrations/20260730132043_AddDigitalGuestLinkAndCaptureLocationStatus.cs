using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalGuestLinkAndCaptureLocationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QrCodes_RestaurantLocationId_QrType",
                table: "QrCodes");

            migrationBuilder.AddColumn<string>(
                name: "CaptureLocationPauseRestoreQrCodeIdsJson",
                table: "RestaurantLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaptureLocationStatus",
                table: "RestaurantLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "QrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkName",
                table: "QrCodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLinkName",
                table: "QrCodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_RestaurantLocationId_NormalizedLinkName",
                table: "QrCodes",
                columns: new[] { "RestaurantLocationId", "NormalizedLinkName" },
                unique: true,
                filter: "[QrType] = 5 AND [Status] IN (0, 1) AND [NormalizedLinkName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_RestaurantLocationId_QrType",
                table: "QrCodes",
                columns: new[] { "RestaurantLocationId", "QrType" },
                unique: true,
                filter: "[Status] IN (0, 1) AND [QrType] <> 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QrCodes_RestaurantLocationId_NormalizedLinkName",
                table: "QrCodes");

            migrationBuilder.DropIndex(
                name: "IX_QrCodes_RestaurantLocationId_QrType",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "CaptureLocationPauseRestoreQrCodeIdsJson",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "CaptureLocationStatus",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "LinkName",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "NormalizedLinkName",
                table: "QrCodes");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_RestaurantLocationId_QrType",
                table: "QrCodes",
                columns: new[] { "RestaurantLocationId", "QrType" },
                unique: true,
                filter: "[Status] IN (0, 1)");
        }
    }
}

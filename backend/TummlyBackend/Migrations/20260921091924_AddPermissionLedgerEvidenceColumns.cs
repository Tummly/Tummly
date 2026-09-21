using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionLedgerEvidenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Basis",
                table: "LocationGuestPermissionLedgerEntries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestFormVersion",
                table: "LocationGuestPermissionLedgerEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyNoticeVersion",
                table: "LocationGuestPermissionLedgerEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WordingSnapshot",
                table: "LocationGuestPermissionLedgerEntries",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WordingVersion",
                table: "LocationGuestPermissionLedgerEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Basis",
                table: "LocationGuestPermissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "GuestFormVersion",
                table: "LocationGuestPermissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "PrivacyNoticeVersion",
                table: "LocationGuestPermissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "WordingSnapshot",
                table: "LocationGuestPermissionLedgerEntries");

            migrationBuilder.DropColumn(
                name: "WordingVersion",
                table: "LocationGuestPermissionLedgerEntries");
        }
    }
}

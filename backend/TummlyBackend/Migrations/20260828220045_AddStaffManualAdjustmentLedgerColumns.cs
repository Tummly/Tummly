using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffManualAdjustmentLedgerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActorStaffUserId",
                table: "CreditLedgerEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HelpCentreQueryId",
                table: "CreditLedgerEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "CreditLedgerEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorStaffUserId",
                table: "CreditLedgerEntries");

            migrationBuilder.DropColumn(
                name: "HelpCentreQueryId",
                table: "CreditLedgerEntries");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "CreditLedgerEntries");
        }
    }
}

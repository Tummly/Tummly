using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBillingAccountPlanAndPricebook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "BillingAccounts",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingStatus",
                table: "BillingAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pilot");

            migrationBuilder.AddColumn<string>(
                name: "ContractedPricebookId",
                table: "BillingAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "TUMMLY-UK-GBP-2026-08-V3");

            migrationBuilder.AddColumn<string>(
                name: "RevolutCustomerId",
                table: "BillingAccounts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StarterKitState",
                table: "BillingAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "unused");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "BillingAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pilot");

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccounts_RevolutCustomerId",
                table: "BillingAccounts",
                column: "RevolutCustomerId",
                unique: true,
                filter: "[RevolutCustomerId] IS NOT NULL AND [RevolutCustomerId] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingAccounts_RevolutCustomerId",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "BillingStatus",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "ContractedPricebookId",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "RevolutCustomerId",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "StarterKitState",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "BillingAccounts");
        }
    }
}

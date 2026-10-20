using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingPendingPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Custom plan columns were in the model snapshot without a migration.
            migrationBuilder.AddColumn<string>(
                name: "CustomPlanConfigJson",
                table: "BillingAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomPlanConfigJson",
                table: "PendingSignups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPaymentCadence",
                table: "BillingAccounts",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPaymentCustomConfigJson",
                table: "BillingAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingPaymentFailedAtUtc",
                table: "BillingAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPaymentFailureCode",
                table: "BillingAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPaymentPlan",
                table: "BillingAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomPlanConfigJson",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CustomPlanConfigJson",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PendingPaymentCadence",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PendingPaymentCustomConfigJson",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PendingPaymentFailedAtUtc",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PendingPaymentFailureCode",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PendingPaymentPlan",
                table: "BillingAccounts");
        }
    }
}

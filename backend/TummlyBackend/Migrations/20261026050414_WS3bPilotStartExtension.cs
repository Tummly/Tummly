using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class WS3bPilotStartExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastPilotExtensionKey",
                table: "BillingAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PilotExtensionCount",
                table: "BillingAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PilotExtensionDaysTotal",
                table: "BillingAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PilotStartedAtUtc",
                table: "BillingAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE BillingAccounts SET PilotStartedAtUtc = DATEADD(day, -30, PilotPeriodEnd) WHERE PilotStartedAtUtc IS NULL AND PilotPeriodEnd IS NOT NULL AND SubscriptionPlan = N'Pilot'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPilotExtensionKey",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PilotExtensionCount",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PilotExtensionDaysTotal",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PilotStartedAtUtc",
                table: "BillingAccounts");
        }
    }
}

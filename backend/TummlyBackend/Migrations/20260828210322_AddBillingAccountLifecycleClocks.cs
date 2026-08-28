using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAccountLifecycleClocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChargebackRestricted",
                table: "BillingAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DormantEnteredAt",
                table: "BillingAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DunningEpisodeStartedAt",
                table: "BillingAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DunningFiredSteps",
                table: "BillingAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PilotDormantNotified",
                table: "BillingAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PilotPeriodEnd",
                table: "BillingAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PilotSoftLockNotified",
                table: "BillingAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoftLockEnteredAt",
                table: "BillingAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE ba
                SET ba.PilotPeriodEnd = u.ActivationExpiresAt
                FROM BillingAccounts AS ba
                INNER JOIN Restaurants AS r ON r.Id = ba.RestaurantId
                INNER JOIN Users AS u ON u.Id = r.OwnerUserId
                WHERE ba.SubscriptionPlan = N'Pilot'
                  AND ba.PilotPeriodEnd IS NULL
                  AND u.ActivationExpiresAt IS NOT NULL;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargebackRestricted",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "DormantEnteredAt",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "DunningEpisodeStartedAt",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "DunningFiredSteps",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PilotDormantNotified",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PilotPeriodEnd",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "PilotSoftLockNotified",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "SoftLockEnteredAt",
                table: "BillingAccounts");
        }
    }
}

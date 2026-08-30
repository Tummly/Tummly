using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAccountScheduledChangeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasScheduledChange",
                table: "BillingAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledCancelPlan",
                table: "BillingAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ScheduledTargetBillingCycle",
                table: "BillingAccounts",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledTargetExtraLocationCount",
                table: "BillingAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduledTargetSubscriptionPlan",
                table: "BillingAccounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasScheduledChange",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "ScheduledCancelPlan",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "ScheduledTargetBillingCycle",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "ScheduledTargetExtraLocationCount",
                table: "BillingAccounts");

            migrationBuilder.DropColumn(
                name: "ScheduledTargetSubscriptionPlan",
                table: "BillingAccounts");
        }
    }
}

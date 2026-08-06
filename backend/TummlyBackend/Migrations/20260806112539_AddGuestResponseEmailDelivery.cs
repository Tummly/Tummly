using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestResponseEmailDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailDeliveredAt",
                table: "FeedbackGuestResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailDeliveryAttemptCount",
                table: "FeedbackGuestResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailDeliveryClaimedAt",
                table: "FeedbackGuestResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailDeliveryRetryAfter",
                table: "FeedbackGuestResponses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailDeliveryStatus",
                table: "FeedbackGuestResponses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackGuestResponses_EmailDeliveryStatus_EmailDeliveryRetryAfter",
                table: "FeedbackGuestResponses",
                columns: new[] { "EmailDeliveryStatus", "EmailDeliveryRetryAfter" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeedbackGuestResponses_EmailDeliveryStatus_EmailDeliveryRetryAfter",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropColumn(
                name: "EmailDeliveredAt",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryAttemptCount",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryClaimedAt",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryRetryAfter",
                table: "FeedbackGuestResponses");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryStatus",
                table: "FeedbackGuestResponses");
        }
    }
}

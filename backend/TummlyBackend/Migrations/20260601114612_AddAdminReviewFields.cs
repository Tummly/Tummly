using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccountCreatedAt",
                table: "TrialRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "TrialRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclineReason",
                table: "TrialRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeclinedAt",
                table: "TrialRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteSentAt",
                table: "TrialRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoreInfoMessage",
                table: "TrialRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MoreInfoRequestedAt",
                table: "TrialRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "TrialRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "TrialRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountCreatedAt",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "DeclineReason",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "DeclinedAt",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "InviteSentAt",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "MoreInfoMessage",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "MoreInfoRequestedAt",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "TrialRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "TrialRequests");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackClassificationDelayedRequeue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassificationDelayedReopenCount",
                table: "Feedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassificationRetryAfter",
                table: "Feedbacks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClassificationRetryable",
                table: "Feedbacks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // ADR-0012 E1: existing Failed rows become retryable and due immediately.
            migrationBuilder.Sql(
                """
                UPDATE Feedbacks
                SET ClassificationRetryable = 1,
                    ClassificationRetryAfter = SYSUTCDATETIME(),
                    ClassificationDelayedReopenCount = 0
                WHERE ClassificationStatus = 2;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassificationDelayedReopenCount",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ClassificationRetryAfter",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ClassificationRetryable",
                table: "Feedbacks");
        }
    }
}

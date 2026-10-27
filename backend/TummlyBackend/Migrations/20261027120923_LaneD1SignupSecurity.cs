using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class LaneD1SignupSecurity : Migration
    {
        /// <summary>
        /// Invalidates tokens and codes issued before lane D1. Each unfinished signup gets a
        /// new session token and no issue time, so the old token no longer matches. Every
        /// open code is marked used, so only codes issued under the new purpose-bound
        /// scheme can verify. Complete rows keep their token.
        /// </summary>
        public const string LegacyTokenInvalidationSql = @"
UPDATE PendingSignups SET SessionToken = NEWID(), SessionTokenIssuedAtUtc = NULL WHERE Status <> 'Complete';
UPDATE OtpVerifications SET IsUsed = 1 WHERE IsUsed = 0;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbuseWindowStartUtc",
                table: "PendingSignups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodeGeneration",
                table: "PendingSignups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CodesSentInWindow",
                table: "PendingSignups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FailedVerifiesInWindow",
                table: "PendingSignups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionTokenIssuedAtUtc",
                table: "PendingSignups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "OtpVerifications",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CodeHash",
                table: "OtpVerifications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "OtpVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Generation",
                table: "OtpVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "OtpVerifications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectId",
                table: "OtpVerifications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtpVerifications_Email_Purpose_IsUsed",
                table: "OtpVerifications",
                columns: new[] { "Email", "Purpose", "IsUsed" });

            migrationBuilder.Sql(LegacyTokenInvalidationSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpVerifications_Email_Purpose_IsUsed",
                table: "OtpVerifications");

            migrationBuilder.DropColumn(
                name: "AbuseWindowStartUtc",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CodeGeneration",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CodesSentInWindow",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "FailedVerifiesInWindow",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "SessionTokenIssuedAtUtc",
                table: "PendingSignups");

            migrationBuilder.DropColumn(
                name: "CodeHash",
                table: "OtpVerifications");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "OtpVerifications");

            migrationBuilder.DropColumn(
                name: "Generation",
                table: "OtpVerifications");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "OtpVerifications");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "OtpVerifications");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "OtpVerifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSignup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RestaurantId",
                table: "RevolutOrderIntents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "PendingSignupId",
                table: "RevolutOrderIntents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingSignups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OtpResendCount = table.Column<int>(type: "int", nullable: false),
                    LastOtpSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TermsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AccountType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ChosenPlan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ChosenCadence = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    OnboardingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevolutOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSignups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevolutOrderIntents_PendingSignupId",
                table: "RevolutOrderIntents",
                column: "PendingSignupId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSignups_SessionToken",
                table: "PendingSignups",
                column: "SessionToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingSignups");

            migrationBuilder.DropIndex(
                name: "IX_RevolutOrderIntents_PendingSignupId",
                table: "RevolutOrderIntents");

            migrationBuilder.DropColumn(
                name: "PendingSignupId",
                table: "RevolutOrderIntents");

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantId",
                table: "RevolutOrderIntents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

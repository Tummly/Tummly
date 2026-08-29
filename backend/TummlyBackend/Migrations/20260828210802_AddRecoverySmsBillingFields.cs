using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoverySmsBillingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingReservationRef",
                table: "FeedbackGuestResponses",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecoverySmsSendIdempotencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReservationRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReservedUnits = table.Column<int>(type: "int", nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoldExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedGuestResponseId = table.Column<int>(type: "int", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoverySmsSendIdempotencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoverySmsSendIdempotencies_FeedbackGuestResponses_CompletedGuestResponseId",
                        column: x => x.CompletedGuestResponseId,
                        principalTable: "FeedbackGuestResponses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecoverySmsSendIdempotencies_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecoverySmsSendIdempotencies_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecoverySmsSendIdempotencies_CompletedGuestResponseId_HoldExpiresAtUtc",
                table: "RecoverySmsSendIdempotencies",
                columns: new[] { "CompletedGuestResponseId", "HoldExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoverySmsSendIdempotencies_FeedbackId",
                table: "RecoverySmsSendIdempotencies",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoverySmsSendIdempotencies_RestaurantId_IdempotencyKey",
                table: "RecoverySmsSendIdempotencies",
                columns: new[] { "RestaurantId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoverySmsSendIdempotencies");

            migrationBuilder.DropColumn(
                name: "BillingReservationRef",
                table: "FeedbackGuestResponses");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackRecoveryOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackRecoveryOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    GuestResponseId = table.Column<int>(type: "int", nullable: true),
                    OfferType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Validity = table.Column<int>(type: "int", nullable: false),
                    ExpiryAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FreeItemText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PurchaseRequirement = table.Column<int>(type: "int", nullable: true),
                    MinimumSpend = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AdditionalExclusions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReplacementItemText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RedemptionCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StaffInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Intent = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackRecoveryOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackRecoveryOffers_FeedbackGuestResponses_GuestResponseId",
                        column: x => x.GuestResponseId,
                        principalTable: "FeedbackGuestResponses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeedbackRecoveryOffers_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackRecoveryOffers_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRecoveryOffers_AuthorUserId",
                table: "FeedbackRecoveryOffers",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRecoveryOffers_FeedbackId_CreatedAt",
                table: "FeedbackRecoveryOffers",
                columns: new[] { "FeedbackId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRecoveryOffers_GuestResponseId",
                table: "FeedbackRecoveryOffers",
                column: "GuestResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRecoveryOffers_RedemptionCode",
                table: "FeedbackRecoveryOffers",
                column: "RedemptionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackRecoveryOffers");
        }
    }
}

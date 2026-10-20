using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkRecoveryBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkRecoveryBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Intent = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Tone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IncludeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkRecoveryBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkRecoveryBatches_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BulkRecoveryBatches_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulkRecoveryBatches_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BulkRecoveryBatchMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FeedbackGuestResponseId = table.Column<int>(type: "int", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkRecoveryBatchMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkRecoveryBatchMembers_BulkRecoveryBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BulkRecoveryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulkRecoveryBatchMembers_FeedbackGuestResponses_FeedbackGuestResponseId",
                        column: x => x.FeedbackGuestResponseId,
                        principalTable: "FeedbackGuestResponses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BulkRecoveryBatchMembers_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkRecoveryBatches_CreatedByUserId",
                table: "BulkRecoveryBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkRecoveryBatches_RestaurantId",
                table: "BulkRecoveryBatches",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkRecoveryBatches_RestaurantLocationId_CreatedAt",
                table: "BulkRecoveryBatches",
                columns: new[] { "RestaurantLocationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkRecoveryBatchMembers_BatchId_FeedbackId",
                table: "BulkRecoveryBatchMembers",
                columns: new[] { "BatchId", "FeedbackId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BulkRecoveryBatchMembers_FeedbackGuestResponseId",
                table: "BulkRecoveryBatchMembers",
                column: "FeedbackGuestResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkRecoveryBatchMembers_FeedbackId",
                table: "BulkRecoveryBatchMembers",
                column: "FeedbackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkRecoveryBatchMembers");

            migrationBuilder.DropTable(
                name: "BulkRecoveryBatches");
        }
    }
}

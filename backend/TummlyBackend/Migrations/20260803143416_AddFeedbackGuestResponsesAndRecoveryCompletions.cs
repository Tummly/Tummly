using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackGuestResponsesAndRecoveryCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackGuestResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Intent = table.Column<int>(type: "int", nullable: false),
                    MaskedDestination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Tone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IncludeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackGuestResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackGuestResponses_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackGuestResponses_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeedbackRecoveryCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    Intent = table.Column<int>(type: "int", nullable: false),
                    WorkflowStatusChangeId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackRecoveryCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackRecoveryCompletions_FeedbackWorkflowStatusChanges_WorkflowStatusChangeId",
                        column: x => x.WorkflowStatusChangeId,
                        principalTable: "FeedbackWorkflowStatusChanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeedbackRecoveryCompletions_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackRecoveryCompletions_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackGuestResponses_AuthorUserId",
                table: "FeedbackGuestResponses",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackGuestResponses_FeedbackId_CreatedAt",
                table: "FeedbackGuestResponses",
                columns: new[] { "FeedbackId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRecoveryCompletions_AuthorUserId",
                table: "FeedbackRecoveryCompletions",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRecoveryCompletions_FeedbackId_CreatedAt",
                table: "FeedbackRecoveryCompletions",
                columns: new[] { "FeedbackId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackRecoveryCompletions_WorkflowStatusChangeId",
                table: "FeedbackRecoveryCompletions",
                column: "WorkflowStatusChangeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackGuestResponses");

            migrationBuilder.DropTable(
                name: "FeedbackRecoveryCompletions");
        }
    }
}

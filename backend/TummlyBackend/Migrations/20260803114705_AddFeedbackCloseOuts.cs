using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackCloseOuts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackCloseOuts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    Intent = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    WorkflowStatusChangeId = table.Column<int>(type: "int", nullable: false),
                    InternalNoteId = table.Column<int>(type: "int", nullable: true),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackCloseOuts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackCloseOuts_FeedbackInternalNotes_InternalNoteId",
                        column: x => x.InternalNoteId,
                        principalTable: "FeedbackInternalNotes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeedbackCloseOuts_FeedbackWorkflowStatusChanges_WorkflowStatusChangeId",
                        column: x => x.WorkflowStatusChangeId,
                        principalTable: "FeedbackWorkflowStatusChanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeedbackCloseOuts_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackCloseOuts_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackCloseOuts_AuthorUserId",
                table: "FeedbackCloseOuts",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackCloseOuts_FeedbackId_CreatedAt",
                table: "FeedbackCloseOuts",
                columns: new[] { "FeedbackId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackCloseOuts_InternalNoteId",
                table: "FeedbackCloseOuts",
                column: "InternalNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackCloseOuts_WorkflowStatusChangeId",
                table: "FeedbackCloseOuts",
                column: "WorkflowStatusChangeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackCloseOuts");
        }
    }
}

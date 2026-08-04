using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackDetectedTagsChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackDetectedTagsChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    FromTagsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ToTagsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FromSentiment = table.Column<int>(type: "int", nullable: true),
                    ToSentiment = table.Column<int>(type: "int", nullable: true),
                    AuthorUserId = table.Column<int>(type: "int", nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackDetectedTagsChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackDetectedTagsChanges_Feedbacks_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackDetectedTagsChanges_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackDetectedTagsChanges_AuthorUserId",
                table: "FeedbackDetectedTagsChanges",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackDetectedTagsChanges_FeedbackId_CreatedAt",
                table: "FeedbackDetectedTagsChanges",
                columns: new[] { "FeedbackId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackDetectedTagsChanges");
        }
    }
}

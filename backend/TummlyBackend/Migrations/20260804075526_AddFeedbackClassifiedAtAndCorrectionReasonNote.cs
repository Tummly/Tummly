using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackClassifiedAtAndCorrectionReasonNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClassifiedAt",
                table: "Feedbacks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Feedbacks
                SET ClassifiedAt = CreatedAt
                WHERE ClassificationStatus = 1;
            ");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "FeedbackClassificationCorrections",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Reason",
                table: "FeedbackClassificationCorrections",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassifiedAt",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "FeedbackClassificationCorrections");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "FeedbackClassificationCorrections");
        }
    }
}

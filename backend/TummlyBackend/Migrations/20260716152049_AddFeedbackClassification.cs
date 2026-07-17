using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassificationStatus",
                table: "Feedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DetectedIssuesJson",
                table: "Feedbacks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sentiment",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            // Pre-pipeline rows were never classified — terminal Failed avoids
            // indefinite Pending empty states on Operator Home.
            migrationBuilder.Sql(
                "UPDATE Feedbacks SET ClassificationStatus = 2"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassificationStatus",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "DetectedIssuesJson",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "Sentiment",
                table: "Feedbacks");
        }
    }
}

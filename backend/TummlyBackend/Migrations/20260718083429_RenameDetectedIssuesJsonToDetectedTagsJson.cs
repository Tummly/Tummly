using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameDetectedIssuesJsonToDetectedTagsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DetectedIssuesJson",
                table: "Feedbacks",
                newName: "DetectedTagsJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DetectedTagsJson",
                table: "Feedbacks",
                newName: "DetectedIssuesJson");
        }
    }
}

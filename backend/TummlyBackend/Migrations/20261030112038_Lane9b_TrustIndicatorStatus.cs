using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class Lane9b_TrustIndicatorStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CtaLabel",
                table: "TrustIndicators",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageRightsNote",
                table: "TrustIndicators",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TrustIndicators",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "QaPending");

            migrationBuilder.AddColumn<string>(
                name: "StatusLabel",
                table: "TrustIndicators",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CtaLabel",
                table: "TrustIndicators");

            migrationBuilder.DropColumn(
                name: "ImageRightsNote",
                table: "TrustIndicators");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrustIndicators");

            migrationBuilder.DropColumn(
                name: "StatusLabel",
                table: "TrustIndicators");
        }
    }
}

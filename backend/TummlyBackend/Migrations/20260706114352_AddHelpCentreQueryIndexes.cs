using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpCentreQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "HelpCentreQueries",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_HelpCentreQueries_Status",
                table: "HelpCentreQueries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HelpCentreQueries_UpdatedAt",
                table: "HelpCentreQueries",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HelpCentreQueries_Status",
                table: "HelpCentreQueries");

            migrationBuilder.DropIndex(
                name: "IX_HelpCentreQueries_UpdatedAt",
                table: "HelpCentreQueries");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "HelpCentreQueries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}

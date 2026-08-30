using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRevolutOrderIntentTopupColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "RevolutOrderIntents",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackLookupKey",
                table: "RevolutOrderIntents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RevolutOrderIntents",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "RevolutOrderIntents");

            migrationBuilder.DropColumn(
                name: "PackLookupKey",
                table: "RevolutOrderIntents");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RevolutOrderIntents");
        }
    }
}

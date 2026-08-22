using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantAnalysisScopeKindAll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScopeKind",
                table: "AssistantMessages",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OwnedLocationId",
                table: "AssistantConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ScopeKind",
                table: "AssistantConversations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "single");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScopeKind",
                table: "AssistantMessages");

            migrationBuilder.DropColumn(
                name: "ScopeKind",
                table: "AssistantConversations");

            migrationBuilder.AlterColumn<int>(
                name: "OwnedLocationId",
                table: "AssistantConversations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

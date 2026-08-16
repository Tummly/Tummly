using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TummlyBackend.Data;

#nullable disable

namespace TummlyBackend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260816170000_AddAssistantDraftInterview")]
    public partial class AddAssistantDraftInterview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftInterviewJson",
                table: "AssistantConversations",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftInterviewJson",
                table: "AssistantConversations");
        }
    }
}

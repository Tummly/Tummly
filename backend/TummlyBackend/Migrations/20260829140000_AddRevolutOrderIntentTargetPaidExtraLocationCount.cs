using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TummlyBackend.Data;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260829140000_AddRevolutOrderIntentTargetPaidExtraLocationCount")]
    public partial class AddRevolutOrderIntentTargetPaidExtraLocationCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetPaidExtraLocationCount",
                table: "RevolutOrderIntents",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetPaidExtraLocationCount",
                table: "RevolutOrderIntents");
        }
    }
}

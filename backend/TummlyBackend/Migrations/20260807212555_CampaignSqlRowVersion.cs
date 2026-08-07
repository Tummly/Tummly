using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class CampaignSqlRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot ALTER int → rowversion. Drop the app-managed int
            // token, then add a DB-managed rowversion (TrialRequest-equivalent).
            // Existing Draft PATCH clients must round-trip the new base64 token.
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Campaigns");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Campaigns",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Campaigns");

            migrationBuilder.AddColumn<int>(
                name: "RowVersion",
                table: "Campaigns",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }
    }
}

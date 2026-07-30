using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodeInternalDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InternalDescription",
                table: "QrCodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternalDescription",
                table: "QrCodes");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodeArchivedLookupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_RestaurantLocationId_Status_ArchivedAt",
                table: "QrCodes",
                columns: new[] { "RestaurantLocationId", "Status", "ArchivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QrCodes_RestaurantLocationId_Status_ArchivedAt",
                table: "QrCodes");
        }
    }
}

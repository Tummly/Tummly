using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationGuestListCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocationGuests_RestaurantLocationId",
                table: "LocationGuests");

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuests_RestaurantLocationId_CreatedAt",
                table: "LocationGuests",
                columns: new[] { "RestaurantLocationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocationGuests_RestaurantLocationId_CreatedAt",
                table: "LocationGuests");

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuests_RestaurantLocationId",
                table: "LocationGuests",
                column: "RestaurantLocationId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedLocationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedLocationId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelectedLocationId",
                table: "Users",
                column: "SelectedLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_RestaurantLocations_SelectedLocationId",
                table: "Users",
                column: "SelectedLocationId",
                principalTable: "RestaurantLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_RestaurantLocations_SelectedLocationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedLocationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedLocationId",
                table: "Users");
        }
    }
}

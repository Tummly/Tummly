using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeInRollout",
                table: "RestaurantLocations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LocalContact",
                table: "RestaurantLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationPhone",
                table: "RestaurantLocations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeInRollout",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "LocalContact",
                table: "RestaurantLocations");

            migrationBuilder.DropColumn(
                name: "LocationPhone",
                table: "RestaurantLocations");
        }
    }
}

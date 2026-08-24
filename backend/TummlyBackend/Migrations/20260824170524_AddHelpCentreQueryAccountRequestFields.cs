using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpCentreQueryAccountRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountRequestKind",
                table: "HelpCentreQueries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "HelpCentreQueries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelpCentreQueries_RestaurantId",
                table: "HelpCentreQueries",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_HelpCentreQueries_Restaurants_RestaurantId",
                table: "HelpCentreQueries",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HelpCentreQueries_Restaurants_RestaurantId",
                table: "HelpCentreQueries");

            migrationBuilder.DropIndex(
                name: "IX_HelpCentreQueries_RestaurantId",
                table: "HelpCentreQueries");

            migrationBuilder.DropColumn(
                name: "AccountRequestKind",
                table: "HelpCentreQueries");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "HelpCentreQueries");
        }
    }
}

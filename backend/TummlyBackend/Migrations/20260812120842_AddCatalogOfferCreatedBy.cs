using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogOfferCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByDisplayName",
                table: "CatalogOffers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "CatalogOffers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogOffers_CreatedByUserId",
                table: "CatalogOffers",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogOffers_Users_CreatedByUserId",
                table: "CatalogOffers",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogOffers_Users_CreatedByUserId",
                table: "CatalogOffers");

            migrationBuilder.DropIndex(
                name: "IX_CatalogOffers_CreatedByUserId",
                table: "CatalogOffers");

            migrationBuilder.DropColumn(
                name: "CreatedByDisplayName",
                table: "CatalogOffers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CatalogOffers");
        }
    }
}

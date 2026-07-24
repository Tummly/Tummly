using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQrScanEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QrScanEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrScanEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrScanEvents_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrScanEvents_RestaurantLocationId_CreatedAt",
                table: "QrScanEvents",
                columns: new[] { "RestaurantLocationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QrScanEvents");
        }
    }
}

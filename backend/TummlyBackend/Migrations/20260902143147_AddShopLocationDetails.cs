using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopLocationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopLocationDetails",
                columns: table => new
                {
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    TableCount = table.Column<int>(type: "int", nullable: false),
                    CounterCount = table.Column<int>(type: "int", nullable: false),
                    EntranceCount = table.Column<int>(type: "int", nullable: false),
                    SecondaryEntranceCount = table.Column<int>(type: "int", nullable: false),
                    TakeawayVolume = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PromptLocations = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExistingMaterials = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopLocationDetails", x => x.LocationId);
                    table.ForeignKey(
                        name: "FK_ShopLocationDetails_RestaurantLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopLocationDetails");
        }
    }
}

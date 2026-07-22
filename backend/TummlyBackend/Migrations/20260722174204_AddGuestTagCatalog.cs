using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestTagCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetectedTagKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AiSourced = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestTags_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationGuestTags",
                columns: table => new
                {
                    LocationGuestId = table.Column<int>(type: "int", nullable: false),
                    GuestTagId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationGuestTags", x => new { x.LocationGuestId, x.GuestTagId });
                    table.ForeignKey(
                        name: "FK_LocationGuestTags_GuestTags_GuestTagId",
                        column: x => x.GuestTagId,
                        principalTable: "GuestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationGuestTags_LocationGuests_LocationGuestId",
                        column: x => x.LocationGuestId,
                        principalTable: "LocationGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestTags_RestaurantId_DetectedTagKey",
                table: "GuestTags",
                columns: new[] { "RestaurantId", "DetectedTagKey" },
                unique: true,
                filter: "[DetectedTagKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GuestTags_RestaurantId_NormalizedName",
                table: "GuestTags",
                columns: new[] { "RestaurantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuestTags_GuestTagId",
                table: "LocationGuestTags",
                column: "GuestTagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationGuestTags");

            migrationBuilder.DropTable(
                name: "GuestTags");
        }
    }
}

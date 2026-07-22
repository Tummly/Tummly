using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterAndLocationGuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocationGuestId",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MasterGuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizedPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterGuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterGuests_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationGuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MasterGuestId = table.Column<int>(type: "int", nullable: false),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OffersOptOut = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationGuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationGuests_MasterGuests_MasterGuestId",
                        column: x => x.MasterGuestId,
                        principalTable: "MasterGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationGuests_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_LocationGuestId",
                table: "Feedbacks",
                column: "LocationGuestId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuests_MasterGuestId_RestaurantLocationId",
                table: "LocationGuests",
                columns: new[] { "MasterGuestId", "RestaurantLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationGuests_RestaurantLocationId",
                table: "LocationGuests",
                column: "RestaurantLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterGuests_RestaurantId_NormalizedEmail",
                table: "MasterGuests",
                columns: new[] { "RestaurantId", "NormalizedEmail" },
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MasterGuests_RestaurantId_NormalizedPhone",
                table: "MasterGuests",
                columns: new[] { "RestaurantId", "NormalizedPhone" },
                unique: true,
                filter: "[NormalizedPhone] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_LocationGuests_LocationGuestId",
                table: "Feedbacks",
                column: "LocationGuestId",
                principalTable: "LocationGuests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_LocationGuests_LocationGuestId",
                table: "Feedbacks");

            migrationBuilder.DropTable(
                name: "LocationGuests");

            migrationBuilder.DropTable(
                name: "MasterGuests");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_LocationGuestId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "LocationGuestId",
                table: "Feedbacks");
        }
    }
}

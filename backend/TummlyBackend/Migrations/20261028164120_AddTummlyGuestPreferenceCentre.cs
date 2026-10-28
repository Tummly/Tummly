using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddTummlyGuestPreferenceCentre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TummlyGuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TummlyGuests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TummlyGuestContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TummlyGuestId = table.Column<int>(type: "int", nullable: false),
                    ContactType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TummlyGuestContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TummlyGuestContacts_TummlyGuests_TummlyGuestId",
                        column: x => x.TummlyGuestId,
                        principalTable: "TummlyGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TummlyGuestLocationMutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TummlyGuestId = table.Column<int>(type: "int", nullable: false),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    MutedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TummlyGuestLocationMutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TummlyGuestLocationMutes_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TummlyGuestLocationMutes_TummlyGuests_TummlyGuestId",
                        column: x => x.TummlyGuestId,
                        principalTable: "TummlyGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TummlyGuestMarketingEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TummlyGuestId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EventKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WordingSnapshot = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TummlyGuestMarketingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TummlyGuestMarketingEvents_TummlyGuests_TummlyGuestId",
                        column: x => x.TummlyGuestId,
                        principalTable: "TummlyGuests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TummlyGuestContacts_ContactType_NormalizedValue",
                table: "TummlyGuestContacts",
                columns: new[] { "ContactType", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TummlyGuestContacts_TummlyGuestId",
                table: "TummlyGuestContacts",
                column: "TummlyGuestId");

            migrationBuilder.CreateIndex(
                name: "IX_TummlyGuestLocationMutes_RestaurantLocationId",
                table: "TummlyGuestLocationMutes",
                column: "RestaurantLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TummlyGuestLocationMutes_TummlyGuestId_RestaurantLocationId",
                table: "TummlyGuestLocationMutes",
                columns: new[] { "TummlyGuestId", "RestaurantLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TummlyGuestMarketingEvents_TummlyGuestId_Channel_OccurredAt",
                table: "TummlyGuestMarketingEvents",
                columns: new[] { "TummlyGuestId", "Channel", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TummlyGuestContacts");

            migrationBuilder.DropTable(
                name: "TummlyGuestLocationMutes");

            migrationBuilder.DropTable(
                name: "TummlyGuestMarketingEvents");

            migrationBuilder.DropTable(
                name: "TummlyGuests");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQrFormOpenAndDemoSessionOverlay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoSessionOverlays",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSessionOverlays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoSessionRegistries",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSessionRegistries", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "QrFormOpenEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    QrCodeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrFormOpenEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrFormOpenEvents_QrCodes_QrCodeId",
                        column: x => x.QrCodeId,
                        principalTable: "QrCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrFormOpenEvents_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoSessionOverlays_ExpiresAtUtc",
                table: "DemoSessionOverlays",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DemoSessionOverlays_SessionId_EntityType_EntityKey",
                table: "DemoSessionOverlays",
                columns: new[] { "SessionId", "EntityType", "EntityKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoSessionRegistries_ExpiresAtUtc",
                table: "DemoSessionRegistries",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_QrFormOpenEvents_QrCodeId",
                table: "QrFormOpenEvents",
                column: "QrCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_QrFormOpenEvents_RestaurantLocationId_CreatedAt",
                table: "QrFormOpenEvents",
                columns: new[] { "RestaurantLocationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoSessionOverlays");

            migrationBuilder.DropTable(
                name: "DemoSessionRegistries");

            migrationBuilder.DropTable(
                name: "QrFormOpenEvents");
        }
    }
}

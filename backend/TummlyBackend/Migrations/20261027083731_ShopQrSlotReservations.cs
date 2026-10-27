using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class ShopQrSlotReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopQrSlotReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantLocationId = table.Column<int>(type: "int", nullable: false),
                    QrType = table.Column<int>(type: "int", nullable: false),
                    ReactivateQrCodeId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ReconcileAfterUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReconcileAttempts = table.Column<int>(type: "int", nullable: false),
                    LastReconcileAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReconcileError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConvertedQrCodeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopQrSlotReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopQrSlotReservations_RestaurantLocations_RestaurantLocationId",
                        column: x => x.RestaurantLocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShopQrSlotReservations_ShopOrders_ShopOrderId",
                        column: x => x.ShopOrderId,
                        principalTable: "ShopOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopQrSlotReservations_RestaurantLocationId_QrType_Status",
                table: "ShopQrSlotReservations",
                columns: new[] { "RestaurantLocationId", "QrType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopQrSlotReservations_ShopOrderId_QrType",
                table: "ShopQrSlotReservations",
                columns: new[] { "ShopOrderId", "QrType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopQrSlotReservations");
        }
    }
}

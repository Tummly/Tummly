using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddShopCartAndShopOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopCarts_RestaurantLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShopCarts_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopCarts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LocationNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlacedByUserId = table.Column<int>(type: "int", nullable: false),
                    PlacedByNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MaterialsNetPence = table.Column<int>(type: "int", nullable: false),
                    VatPence = table.Column<int>(type: "int", nullable: false),
                    DeliveryNetPence = table.Column<int>(type: "int", nullable: false),
                    GrossPence = table.Column<int>(type: "int", nullable: false),
                    DeliveryMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RevolutOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FulfilmentStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TrackingUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    OpsNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ShipToContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShipToContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ShipToAddressLine1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ShipToAddressLine2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShipToPostcode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ShipToCountry = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeliveryInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOrders_RestaurantLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "RestaurantLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShopOrders_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopOrders_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShopOrders_Users_PlacedByUserId",
                        column: x => x.PlacedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopOrderSequences",
                columns: table => new
                {
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    NextNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOrderSequences", x => x.RestaurantId);
                    table.ForeignKey(
                        name: "FK_ShopOrderSequences_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopCartLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopCartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCartLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopCartLines_ShopCarts_ShopCartId",
                        column: x => x.ShopCartId,
                        principalTable: "ShopCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogSkuId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TitleSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaterialType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitNetPence = table.Column<int>(type: "int", nullable: false),
                    LineNetPence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOrderLines_ShopOrders_ShopOrderId",
                        column: x => x.ShopOrderId,
                        principalTable: "ShopOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopCartLines_ShopCartId_SkuId",
                table: "ShopCartLines",
                columns: new[] { "ShopCartId", "SkuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopCarts_LocationId",
                table: "ShopCarts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCarts_RestaurantId_LocationId_UserId",
                table: "ShopCarts",
                columns: new[] { "RestaurantId", "LocationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopCarts_UserId",
                table: "ShopCarts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrderLines_ShopOrderId",
                table: "ShopOrderLines",
                column: "ShopOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_CancelledByUserId",
                table: "ShopOrders",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_LocationId",
                table: "ShopOrders",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_PlacedByUserId",
                table: "ShopOrders",
                column: "PlacedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_RestaurantId_LocationId_CreatedAtUtc",
                table: "ShopOrders",
                columns: new[] { "RestaurantId", "LocationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_RestaurantId_OrderNumber",
                table: "ShopOrders",
                columns: new[] { "RestaurantId", "OrderNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopCartLines");

            migrationBuilder.DropTable(
                name: "ShopOrderLines");

            migrationBuilder.DropTable(
                name: "ShopOrderSequences");

            migrationBuilder.DropTable(
                name: "ShopCarts");

            migrationBuilder.DropTable(
                name: "ShopOrders");
        }
    }
}

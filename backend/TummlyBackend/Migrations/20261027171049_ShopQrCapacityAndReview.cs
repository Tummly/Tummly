using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class ShopQrCapacityAndReview : Migration
    {
        // Data-only: table tents, table talkers, window stickers and guest cards
        // mint QR codes at Shop fulfilment (QR path for Admin availability).
        public const string EnableMintOnShopFulfilmentSql =
            """
            UPDATE [ShopMaterialSkus]
            SET [MintOnShopFulfilment] = CAST(1 AS bit)
            WHERE [SkuId] IN (N'table-tents', N'table-talker', N'window-stickers', N'qr-guest-cards')
              AND [Status] <> N'Deleted';
            """;

        public const string DisableMintOnShopFulfilmentSql =
            """
            UPDATE [ShopMaterialSkus]
            SET [MintOnShopFulfilment] = CAST(0 AS bit)
            WHERE [SkuId] IN (N'table-tents', N'table-talker', N'window-stickers', N'qr-guest-cards')
              AND [Status] <> N'Deleted';
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "ShopOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

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

            migrationBuilder.Sql(EnableMintOnShopFulfilmentSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DisableMintOnShopFulfilmentSql);

            migrationBuilder.DropTable(
                name: "ShopQrSlotReservations");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "ShopOrders");
        }
    }
}

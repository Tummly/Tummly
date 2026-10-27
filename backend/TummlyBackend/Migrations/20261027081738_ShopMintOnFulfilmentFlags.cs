using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class ShopMintOnFulfilmentFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only: table tents, table talkers, window stickers and guest cards
            // mint QR codes at Shop fulfilment (QR path for Admin availability).
            migrationBuilder.Sql(
                """
                UPDATE [ShopMaterialSkus]
                SET [MintOnShopFulfilment] = CAST(1 AS bit)
                WHERE [SkuId] IN (N'table-tents', N'table-talker', N'window-stickers', N'qr-guest-cards')
                  AND [Status] <> N'Deleted';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [ShopMaterialSkus]
                SET [MintOnShopFulfilment] = CAST(0 AS bit)
                WHERE [SkuId] IN (N'table-tents', N'table-talker', N'window-stickers', N'qr-guest-cards')
                  AND [Status] <> N'Deleted';
                """
            );
        }
    }
}

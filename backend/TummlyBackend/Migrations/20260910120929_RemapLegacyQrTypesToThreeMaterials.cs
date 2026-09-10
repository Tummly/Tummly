using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemapLegacyQrTypesToThreeMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             Supported defaults: TableTent=7, WindowSticker=3, OfferCard=8.
             Legacy: CounterCard=0, PackagingSticker=1, DeliveryInsert=2,
             SmartGuest=4, ReceiptSticker=6. DigitalGuestLink=5 unchanged.
             Status: Active=0, Paused=1, Archived=2.
             Unique live slot: (location, QrType) where Status IN (0,1).
            */

            // 1) Remap live SmartGuest → TableTent when no live TableTent exists.
            // Keeps the same Token so printed / Home guest URLs stay valid.
            migrationBuilder.Sql(
                """
                UPDATE q
                SET q.[QrType] = 7
                FROM [QrCodes] AS q
                WHERE q.[QrType] = 4
                  AND q.[Status] IN (0, 1)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [QrCodes] AS t
                      WHERE t.[RestaurantLocationId] = q.[RestaurantLocationId]
                        AND t.[QrType] = 7
                        AND t.[Status] IN (0, 1)
                  );
                """
            );

            // 2) Remap live CounterCard → OfferCard when no live OfferCard exists.
            migrationBuilder.Sql(
                """
                UPDATE q
                SET q.[QrType] = 8
                FROM [QrCodes] AS q
                WHERE q.[QrType] = 0
                  AND q.[Status] IN (0, 1)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [QrCodes] AS o
                      WHERE o.[RestaurantLocationId] = q.[RestaurantLocationId]
                        AND o.[QrType] = 8
                        AND o.[Status] IN (0, 1)
                  );
                """
            );

            // 3a) Delete idle leftover legacy rows (no Feedback, no QrScanEvent).
            // Feedback/QrScanEvent FKs are Restrict — only idle rows may delete.
            migrationBuilder.Sql(
                """
                DELETE q
                FROM [QrCodes] AS q
                WHERE q.[QrType] IN (0, 1, 2, 4, 6)
                  AND NOT EXISTS (
                      SELECT 1 FROM [Feedbacks] AS f WHERE f.[QrCodeId] = q.[Id]
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [QrScanEvents] AS s
                      WHERE s.[QrCodeId] = q.[Id]
                  );
                """
            );

            // 3b) Archive leftover legacy rows that still have history.
            migrationBuilder.Sql(
                """
                UPDATE q
                SET q.[Status] = 2,
                    q.[ArchivedAt] = GETUTCDATE(),
                    q.[ArchivedByDisplayName] = N'system-migration',
                    q.[UpdatedAt] = GETUTCDATE()
                FROM [QrCodes] AS q
                WHERE q.[QrType] IN (0, 1, 2, 4, 6)
                  AND q.[Status] IN (0, 1);
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reverse of remaps only. Deleted idle rows and
            // archived history rows are not restored.
            migrationBuilder.Sql(
                """
                UPDATE q
                SET q.[QrType] = 4
                FROM [QrCodes] AS q
                WHERE q.[QrType] = 7
                  AND q.[Status] IN (0, 1)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [QrCodes] AS s
                      WHERE s.[RestaurantLocationId] = q.[RestaurantLocationId]
                        AND s.[QrType] = 4
                        AND s.[Status] IN (0, 1)
                  );
                """
            );

            migrationBuilder.Sql(
                """
                UPDATE q
                SET q.[QrType] = 0
                FROM [QrCodes] AS q
                WHERE q.[QrType] = 8
                  AND q.[Status] IN (0, 1)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [QrCodes] AS c
                      WHERE c.[RestaurantLocationId] = q.[RestaurantLocationId]
                        AND c.[QrType] = 0
                        AND c.[Status] IN (0, 1)
                  );
                """
            );
        }
    }
}

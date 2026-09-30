using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddThankYouOfferIssueUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // QA (and any env that issued thank-you twice before this constraint)
            // has duplicate (LocationGuestId, CatalogOfferId) rows. CreateIndex
            // fails with SQL 1505 unless we keep one row per pair first.
            // Prefer: void-request linked, then redeemed, then lowest Id.
            migrationBuilder.Sql(
                """
                ;WITH ranked AS (
                    SELECT
                        oi.[Id],
                        oi.[LocationGuestId],
                        oi.[CatalogOfferId],
                        ROW_NUMBER() OVER (
                            PARTITION BY oi.[LocationGuestId], oi.[CatalogOfferId]
                            ORDER BY
                                CASE WHEN EXISTS (
                                    SELECT 1
                                    FROM [OfferVoidRequests] AS v
                                    WHERE v.[OfferIssueId] = oi.[Id]
                                ) THEN 0 ELSE 1 END,
                                CASE WHEN oi.[RedeemedAtUtc] IS NOT NULL THEN 0 ELSE 1 END,
                                oi.[Id]
                        ) AS [rn]
                    FROM [OfferIssues] AS oi
                    WHERE oi.[Source] = N'guest_form_thank_you'
                ),
                keepers AS (
                    SELECT [Id], [LocationGuestId], [CatalogOfferId]
                    FROM ranked
                    WHERE [rn] = 1
                ),
                losers AS (
                    SELECT [Id], [LocationGuestId], [CatalogOfferId]
                    FROM ranked
                    WHERE [rn] > 1
                )
                UPDATE v
                SET v.[OfferIssueId] = k.[Id]
                FROM [OfferVoidRequests] AS v
                INNER JOIN losers AS l ON l.[Id] = v.[OfferIssueId]
                INNER JOIN keepers AS k
                    ON k.[LocationGuestId] = l.[LocationGuestId]
                   AND k.[CatalogOfferId] = l.[CatalogOfferId];

                ;WITH ranked AS (
                    SELECT
                        oi.[Id],
                        ROW_NUMBER() OVER (
                            PARTITION BY oi.[LocationGuestId], oi.[CatalogOfferId]
                            ORDER BY
                                CASE WHEN EXISTS (
                                    SELECT 1
                                    FROM [OfferVoidRequests] AS v
                                    WHERE v.[OfferIssueId] = oi.[Id]
                                ) THEN 0 ELSE 1 END,
                                CASE WHEN oi.[RedeemedAtUtc] IS NOT NULL THEN 0 ELSE 1 END,
                                oi.[Id]
                        ) AS [rn]
                    FROM [OfferIssues] AS oi
                    WHERE oi.[Source] = N'guest_form_thank_you'
                )
                DELETE FROM [OfferIssues]
                WHERE [Id] IN (SELECT [Id] FROM ranked WHERE [rn] > 1);
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues",
                columns: new[] { "LocationGuestId", "CatalogOfferId" },
                unique: true,
                filter: "[Source] = N'guest_form_thank_you'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfferIssues_ThankYou_LocationGuestId_CatalogOfferId",
                table: "OfferIssues");
        }
    }
}

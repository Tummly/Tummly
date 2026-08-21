using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TummlyBackend.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Active = in-flight: demote unattached <b>open</b> Active → Draft; promote
    /// Draft rows that already have a live attach → Active. Leaves expired-by-date
    /// Active rows alone (Sent / expired wire status).
    /// </summary>
    public partial class CatalogOfferActiveMeansInFlight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Validity ChooseExpiryDate = 3 (CatalogOfferValidity).
            migrationBuilder.Sql(@"
;WITH LiveAttachOfferIds AS (
    SELECT OfferId = c.OfferId
    FROM Campaigns AS c
    WHERE c.OfferId IS NOT NULL
    UNION
    SELECT f.RecoveryOfferId
    FROM Feedbacks AS f
    WHERE f.RecoveryOfferId IS NOT NULL
    UNION
    SELECT l.ThankYouCatalogOfferId
    FROM RestaurantLocations AS l
    WHERE l.ThankYouCatalogOfferId IS NOT NULL
)
UPDATE o
SET o.Status = 'draft', o.UpdatedAt = SYSUTCDATETIME()
FROM CatalogOffers AS o
WHERE o.Status = 'active'
  AND NOT EXISTS (
        SELECT 1 FROM LiveAttachOfferIds AS a WHERE a.OfferId = o.Id
      )
  AND NOT (
        o.Validity = 3
        AND o.CustomExpiryDate IS NOT NULL
        AND o.CustomExpiryDate < CAST(SYSUTCDATETIME() AS date)
      );

;WITH LiveAttachOfferIds AS (
    SELECT OfferId = c.OfferId
    FROM Campaigns AS c
    WHERE c.OfferId IS NOT NULL
    UNION
    SELECT f.RecoveryOfferId
    FROM Feedbacks AS f
    WHERE f.RecoveryOfferId IS NOT NULL
    UNION
    SELECT l.ThankYouCatalogOfferId
    FROM RestaurantLocations AS l
    WHERE l.ThankYouCatalogOfferId IS NOT NULL
)
UPDATE o
SET o.Status = 'active', o.UpdatedAt = SYSUTCDATETIME()
FROM CatalogOffers AS o
WHERE o.Status = 'draft'
  AND EXISTS (
        SELECT 1 FROM LiveAttachOfferIds AS a WHERE a.OfferId = o.Id
      );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: prior Active-without-attach rows cannot be restored.
        }
    }
}

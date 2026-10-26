using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Billing.Pricebook
{
    /// <summary>
    /// Publishes the provisional Location band rates once, for databases seeded before bands existed.
    /// Copies the latest Published version, so every Admin-edited value is kept.
    /// </summary>
    public static class CommercialCatalogBandRatesUpgrade
    {
        public const string PublicId = "TUMMLY-UK-GBP-BANDS-20261026";

        public const string Notes = "Location bands (provisional, doc 08 R3)";

        /// <summary>
        /// Runs <see cref="EnsureAsync"/> in its own scope and context, so a failed save cannot
        /// leave tracked entries on the shared startup context. Logs and returns false on failure.
        /// </summary>
        public static async Task<bool> TryEnsureInOwnScopeAsync(
            IServiceProvider services,
            ILogger logger,
            CancellationToken ct = default
        )
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await EnsureAsync(db, DateTime.UtcNow, logger, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Commercial catalogue band-rate upgrade failed; continuing without it."
                );
                return false;
            }
        }

        /// <summary>
        /// Returns true when it published a new version. No-op when the latest Published version
        /// already has all four band rows, or when the upgrade version already exists.
        /// </summary>
        public static async Task<bool> EnsureAsync(
            ApplicationDbContext db,
            DateTime nowUtc,
            ILogger logger,
            CancellationToken ct = default
        )
        {
            if (
                await db.CommercialCatalogVersions.AnyAsync(
                    v => v.PublicId == PublicId,
                    ct
                )
            )
            {
                return false;
            }

            var published = await db
                .CommercialCatalogVersions.Include(v => v.Plans)
                .Include(v => v.TopUpPacks)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates)
                .Where(v => v.Status == CommercialCatalogStatuses.Published)
                .OrderByDescending(v => v.PublishedAtUtc)
                .ToListAsync(ct);

            var latest = published.FirstOrDefault();
            if (latest == null)
            {
                return false;
            }

            var bandRates = LocationBandPricing.Bands.Where(b => b.Index > 1).ToList();
            if (
                bandRates.All(band =>
                    latest.CustomUnitRates.Any(r => r.RateKey == band.RateKey)
                )
            )
            {
                return false;
            }

            var previousPublicId = latest.PublicId;
            var version = CommercialCatalogMapper.CloneAsDraft(latest, nowUtc);

            foreach (var band in bandRates)
            {
                if (version.CustomUnitRates.Any(r => r.RateKey == band.RateKey))
                {
                    continue;
                }

                var rate = LocationBandPricing.ProvisionalRates[band.Index];
                version.CustomUnitRates.Add(
                    new CommercialCustomUnitRateRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = version.Id,
                        RateKey = band.RateKey,
                        UnitQuantity = 1,
                        MonthlyNetPence = rate.MonthlyNetPence,
                        AnnualNetPence = rate.AnnualNetPence,
                    }
                );
            }

            foreach (var row in published)
            {
                row.Status = CommercialCatalogStatuses.Superseded;
            }

            version.PublicId = PublicId;
            version.Status = CommercialCatalogStatuses.Published;
            version.Notes = Notes;
            version.PublishedAtUtc = nowUtc;
            version.UpdatedAtUtc = nowUtc;

            db.CommercialCatalogVersions.Add(version);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Published Location band rates. Previous publicId {PreviousPublicId}, new publicId {NewPublicId}.",
                previousPublicId,
                PublicId
            );
            return true;
        }
    }
}

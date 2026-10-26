using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Billing.Pricebook
{
    /// <summary>
    /// Seeds the first Published commercial catalog from the v3 pack JSON when empty.
    /// </summary>
    public static class CommercialCatalogSeeder
    {
        public static async Task EnsureSeededAsync(
            ApplicationDbContext db,
            IHostEnvironment environment,
            CancellationToken cancellationToken = default
        )
        {
            if (await db.CommercialCatalogVersions.AnyAsync(cancellationToken))
            {
                return;
            }

            var jsonCatalog = PricebookCatalog.CreateForHost(environment);
            var snapshot = jsonCatalog.GetRequired(jsonCatalog.CurrentPricebookId);
            var now = DateTime.UtcNow;

            var version = FromSnapshot(snapshot, now);
            db.CommercialCatalogVersions.Add(version);
            await db.SaveChangesAsync(cancellationToken);
        }

        public static CommercialCatalogVersion FromSnapshot(
            PricebookSnapshot snapshot,
            DateTime nowUtc
        )
        {
            var version = new CommercialCatalogVersion
            {
                Id = Guid.NewGuid(),
                PublicId = snapshot.Id,
                Status = CommercialCatalogStatuses.Published,
                PublishedAtUtc = nowUtc,
                VatRateBps = snapshot.VatRateBps,
                Notes = "Seeded from billing pack v3.0 JSON",
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,
            };

            foreach (var (key, plan) in snapshot.Plans)
            {
                version.Plans.Add(
                    new CommercialPlanRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = version.Id,
                        PlanKey = key,
                        DisplayName = plan.DisplayName,
                        MonthlyNetPence = plan.MonthlyNetPence,
                        AnnualNetPence = plan.AnnualNetPence,
                        IncludedLocations = plan.IncludedLocations,
                        IncludedTeamMembers = plan.IncludedTeamMembers,
                        ActiveOffersAccount = plan.ActiveOffersAccount,
                        ActiveQrPlacementsPerLocation =
                            plan.ActiveQrPlacementsPerLocation,
                        PublishedGuestFormsPerLocation =
                            plan.PublishedGuestFormsPerLocation,
                        DraftGuestFormsPerLocation =
                            plan.DraftGuestFormsPerLocation,
                        CreditsMonthlyAi = plan.CreditsMonthly?.Ai,
                        CreditsMonthlyEmail = plan.CreditsMonthly?.Email,
                        CreditsMonthlySms = plan.CreditsMonthly?.Sms,
                        CreditsOneTimeAi = plan.CreditsOneTime?.Ai,
                        CreditsOneTimeEmail = plan.CreditsOneTime?.Email,
                        CreditsOneTimeSms = plan.CreditsOneTime?.Sms,
                        LookupKeyMonthly = RevolutPlanVariationKeys.ForPlanCadence(
                            plan.DisplayName,
                            "monthly"
                        ),
                        LookupKeyAnnual = RevolutPlanVariationKeys.ForPlanCadence(
                            plan.DisplayName,
                            "annual"
                        ),
                        SelfServeMaxLocations = key == "group"
                            ? LocationCap.GroupSelfServeMax
                            : null,
                    }
                );
            }

            foreach (var pack in snapshot.TopUpPacks)
            {
                version.TopUpPacks.Add(
                    new CommercialTopUpPackRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = version.Id,
                        Channel = pack.Channel,
                        Quantity = pack.Quantity,
                        NetPence = pack.NetPence,
                        LookupKey = pack.LookupKey,
                        ApprovalRequired = pack.ApprovalRequired,
                    }
                );
            }

            if (snapshot.ExtraGroupLocationMonthlyNetPence != null)
            {
                version.ExtraLocations.Add(
                    new CommercialExtraLocationRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = version.Id,
                        MonthlyNetPence =
                            snapshot.ExtraGroupLocationMonthlyNetPence.Value,
                        AnnualNetPence =
                            snapshot.ExtraGroupLocationAnnualNetPence ?? 0,
                        UsersAdded = 2,
                        CreditsMonthlyAi =
                            snapshot.ExtraLocationCreditsMonthly?.Ai ?? 0,
                        CreditsMonthlyEmail =
                            snapshot.ExtraLocationCreditsMonthly?.Email ?? 0,
                        CreditsMonthlySms =
                            snapshot.ExtraLocationCreditsMonthly?.Sms ?? 0,
                        LookupKeyMonthly =
                            RevolutPlanVariationKeys.GroupLocationMonthly,
                        LookupKeyAnnual =
                            RevolutPlanVariationKeys.GroupLocationAnnual,
                    }
                );
            }

            // Placeholder unit rates (Admin replaces with signed sheet).
            // Pack sizes match Figma full calculator `6657:37518`.
            AddRate(version, CommercialCustomUnitRateKeys.Location, 1, 3900, 39800);
            foreach (var band in LocationBandPricing.Bands.Where(b => b.Index > 1))
            {
                var rate = LocationBandPricing.ProvisionalRates[band.Index];
                AddRate(
                    version,
                    band.RateKey,
                    1,
                    rate.MonthlyNetPence,
                    rate.AnnualNetPence
                );
            }

            AddRate(version, CommercialCustomUnitRateKeys.TeamUser, 5, 1500, null);
            AddRate(version, CommercialCustomUnitRateKeys.QrPlacement, 10, 600, null);
            AddRate(version, CommercialCustomUnitRateKeys.Offer, 5, 1200, null);
            AddRate(version, CommercialCustomUnitRateKeys.EmailCredit, 1000, 120, null);
            AddRate(version, CommercialCustomUnitRateKeys.SmsCredit, 1, 10, null);
            AddRate(version, CommercialCustomUnitRateKeys.AiCredit, 1, 3, null);

            return version;
        }

        private static void AddRate(
            CommercialCatalogVersion version,
            string key,
            int unitQuantity,
            int monthlyNetPence,
            int? annualNetPence
        )
        {
            version.CustomUnitRates.Add(
                new CommercialCustomUnitRateRow
                {
                    Id = Guid.NewGuid(),
                    CatalogVersionId = version.Id,
                    RateKey = key,
                    UnitQuantity = unitQuantity,
                    MonthlyNetPence = monthlyNetPence,
                    AnnualNetPence = annualNetPence,
                }
            );
        }
    }
}

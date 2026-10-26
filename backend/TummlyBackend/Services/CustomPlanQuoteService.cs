using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class CustomPlanQuoteService : ICustomPlanQuoteService
    {
        private static readonly string[] PaidPlanKeys = ["starter", "growth", "group"];

        private readonly ApplicationDbContext _context;

        public CustomPlanQuoteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomPlanQuoteResult> QuoteAsync(
            CustomPlanQuoteRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var cadence = string.Equals(
                request.Cadence?.Trim(),
                "annual",
                StringComparison.OrdinalIgnoreCase
            )
                ? "annual"
                : "monthly";

            // Validate the request before any catalogue read or maths.
            var locations = ValidateLocations(request.Locations);
            var packs = NormalizePacks(request.Packs);

            var version = await LoadPublishedAsync(cancellationToken)
                ?? throw new CustomPlanQuoteException(
                    "catalog_not_ready",
                    "Catalog not ready."
                );

            var plans = BuildPlanSummaries(version);
            if (plans.Count == 0)
            {
                throw new CustomPlanQuoteException(
                    "catalog_not_ready",
                    "Catalog not ready."
                );
            }

            var rates = version.CustomUnitRates
                .Select(r => new CustomPlanRateSnapshot
                {
                    RateKey = r.RateKey,
                    UnitQuantity = r.UnitQuantity,
                    MonthlyNetPence = r.MonthlyNetPence,
                    AnnualNetPence = r.AnnualNetPence,
                })
                .ToList();
            var ratesByKey = rates.ToDictionary(
                r => r.RateKey,
                StringComparer.Ordinal
            );

            var minPlan = MinBasePlanForLocations(locations, plans);
            // Location count alone sets the base package; other capacity is
            // bought as packs on top (never by moving to a higher package).
            var requested = ParseBasePlanKey(request.BasePlan);
            var basePlan = minPlan;

            var planRow = plans.FirstOrDefault(p =>
                string.Equals(p.PlanKey, basePlan, StringComparison.Ordinal)
            );
            if (planRow == null)
            {
                throw new CustomPlanQuoteException(
                    "invalid_base_plan",
                    "Unknown base plan."
                );
            }

            if (
                packs.GetValueOrDefault("sms_5000") > 0
                && !string.Equals(basePlan, "group", StringComparison.Ordinal)
            )
            {
                throw new CustomPlanQuoteException(
                    "sms_pack_requires_approval",
                    "The 5,000 SMS pack needs a Group plan or sales approval."
                );
            }

            var basePlanChanged =
                requested != null
                && !string.Equals(requested, basePlan, StringComparison.Ordinal);

            var includedLocations = planRow.IncludedLocations;
            var additionalLocations = Math.Max(0, locations - includedLocations);
            var extra = version.ExtraLocations.FirstOrDefault();
            var usersAdded = extra?.UsersAdded ?? 0;
            var extraAi = extra?.CreditsMonthlyAi ?? 0;
            var extraEmail = extra?.CreditsMonthlyEmail ?? 0;
            var extraSms = extra?.CreditsMonthlySms ?? 0;

            var teamFromPacks = packs.GetValueOrDefault("team_users_5") * 5;
            var offersFromPacks = packs.GetValueOrDefault("offers_5") * 5;
            var qrAccountExtra = packs.GetValueOrDefault("qr_10") * 10;
            var emailFromPacks =
                packs.GetValueOrDefault("email_20k") * 20_000
                + packs.GetValueOrDefault("email_50k") * 50_000;
            var smsFromPacks =
                packs.GetValueOrDefault("sms_500") * 500
                + packs.GetValueOrDefault("sms_1000") * 1_000
                + packs.GetValueOrDefault("sms_5000") * 5_000;
            var aiFromPacks =
                packs.GetValueOrDefault("ai_500") * 500
                + packs.GetValueOrDefault("ai_2000") * 2_000;

            var quantities = new CustomPlanQuantities
            {
                Locations = locations,
                TeamUsers =
                    planRow.IncludedTeamMembers
                    + teamFromPacks
                    + additionalLocations * usersAdded,
                QrPlacements = planRow.ActiveQrPlacementsPerLocation,
                QrPlacementsAccountExtra = qrAccountExtra,
                Offers = planRow.ActiveOffersAccount + offersFromPacks,
                EmailCredits =
                    planRow.CreditsMonthlyEmail
                    + emailFromPacks
                    + additionalLocations * extraEmail,
                SmsCredits =
                    planRow.CreditsMonthlySms
                    + smsFromPacks
                    + additionalLocations * extraSms,
                AiCredits =
                    planRow.CreditsMonthlyAi
                    + aiFromPacks
                    + additionalLocations * extraAi,
            };

            var lines = new List<CustomPlanLine>();
            var monthlyNetPence = 0;
            var periodNetPence = 0;

            var monthlyBase = planRow.MonthlyNetPence;
            var periodBase = PlanPeriodNetPence(planRow, cadence);
            monthlyNetPence += monthlyBase;
            periodNetPence += periodBase;
            lines.Add(
                new CustomPlanLine
                {
                    Key = $"base:{basePlan}",
                    Label =
                        $"{planRow.DisplayName} · {includedLocations} included Location{(includedLocations == 1 ? "" : "s")}",
                    Quantity = 1,
                    NetPence = periodBase,
                }
            );

            var ratesByBand = LocationBandRates(ratesByKey);
            foreach (
                var slice in LocationBandPricing.Slice(includedLocations + 1, locations)
            )
            {
                if (
                    !LocationBandPricing.TryPrice(
                        slice.FirstSlot,
                        slice.LastSlot,
                        cadence,
                        ratesByBand,
                        out var periodExtra,
                        out _
                    )
                )
                {
                    throw new CustomPlanQuoteException(
                        "catalog_incomplete",
                        "The Location price is missing from the catalogue."
                    );
                }

                var bandRate = ratesByBand[slice.BandIndex];
                monthlyNetPence += CustomPlanLineNet.Monthly(
                    slice.SlotCount,
                    bandRate.MonthlyNetPence,
                    1
                );
                periodNetPence += periodExtra;
                lines.Add(
                    new CustomPlanLine
                    {
                        Key = $"extra:location:band_{slice.BandIndex}",
                        Label =
                            $"{slice.SlotCount} additional Location{(slice.SlotCount == 1 ? "" : "s")} ({slice.FirstSlot}–{slice.LastSlot})",
                        Quantity = slice.SlotCount,
                        NetPence = periodExtra,
                    }
                );
            }

            foreach (var def in CustomPlanPacks.All)
            {
                var count = packs.GetValueOrDefault(def.PackKey);
                if (count <= 0)
                {
                    continue;
                }

                if (!ratesByKey.TryGetValue(def.RateKey, out var rate))
                {
                    continue;
                }

                var naturalQty = count * def.PackSize;
                var monthlyPack = CustomPlanLineNet.Monthly(
                    naturalQty,
                    rate.MonthlyNetPence,
                    rate.UnitQuantity
                );
                var periodPack = CustomPlanLineNet.ForCadence(
                    cadence,
                    naturalQty,
                    rate.MonthlyNetPence,
                    rate.AnnualNetPence,
                    rate.UnitQuantity
                );
                monthlyNetPence += monthlyPack;
                periodNetPence += periodPack;
                lines.Add(
                    new CustomPlanLine
                    {
                        Key = $"pack:{def.PackKey}",
                        Label = $"{PackTitle(def.PackKey)} × {count}",
                        Quantity = count,
                        NetPence = periodPack,
                    }
                );
            }

            var packsHaveAny = packs.Values.Any(c => c > 0);
            var plainCatalog = !packsHaveAny && additionalLocations == 0;
            var storedAsPlan = plainCatalog
                ? BasePlanToChosenPlan(basePlan)
                : BillingSubscriptionPlans.Custom;

            // Compact packs: omit zero counts in stored JSON.
            var storedPacks = packs
                .Where(kv => kv.Value > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

            var quote = new CustomPlanQuoteResult
            {
                Version = 2,
                CatalogPublicId = version.PublicId,
                Cadence = cadence,
                BasePlan = basePlan,
                Locations = locations,
                Packs = storedPacks,
                Quantities = quantities,
                MonthlyNetPence = monthlyNetPence,
                PeriodNetPence = periodNetPence,
                Lines = lines,
                RateSnapshot = rates,
                BasePlanChanged = basePlanChanged,
                RequestedBasePlan = requested,
                StoredAsPlan = storedAsPlan,
                VatRateBps = version.VatRateBps,
            };
            quote.QuoteHash = CustomPlanQuoteHash.Compute(quote);
            return quote;
        }

        private async Task<CommercialCatalogVersion?> LoadPublishedAsync(
            CancellationToken cancellationToken
        )
        {
            return await _context.CommercialCatalogVersions
                .AsNoTracking()
                .Include(v => v.Plans)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates)
                .Where(v => v.Status == CommercialCatalogStatuses.Published)
                .OrderByDescending(v => v.PublishedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        internal static int ValidateLocations(decimal locations)
        {
            if (locations < 1 || locations != decimal.Truncate(locations))
            {
                throw new CustomPlanQuoteException(
                    "invalid_location_count",
                    "Enter a whole number of Locations."
                );
            }

            if (locations > CustomPlanConfig.MaxLocations)
            {
                throw new CustomPlanQuoteException(
                    "custom_plan_contact_sales",
                    "More than 500 Locations needs a sales conversation."
                );
            }

            return (int)locations;
        }

        internal static Dictionary<string, int> NormalizePacks(
            Dictionary<string, decimal>? packs
        )
        {
            var next = CustomPlanPacks.All.ToDictionary(
                p => p.PackKey,
                _ => 0,
                StringComparer.Ordinal
            );
            if (packs == null)
            {
                return next;
            }

            // Unknown keys first (across all entries), then quantities.
            foreach (var key in packs.Keys)
            {
                if (!CustomPlanPacks.IsKnownPackKey(key))
                {
                    throw new CustomPlanQuoteException(
                        "invalid_pack",
                        $"Unknown pack key: {key}."
                    );
                }
            }

            foreach (var (key, raw) in packs)
            {
                if (
                    raw < 0
                    || raw > CustomPlanPacks.MaxPackCount
                    || raw != decimal.Truncate(raw)
                )
                {
                    throw new CustomPlanQuoteException(
                        "invalid_pack_quantity",
                        $"Pack {key} must be a whole number from 0 to {CustomPlanPacks.MaxPackCount}."
                    );
                }

                next[key] = (int)raw;
            }

            return next;
        }

        private static Dictionary<int, LocationBandRate> LocationBandRates(
            IReadOnlyDictionary<string, CustomPlanRateSnapshot> ratesByKey
        )
        {
            var bands = new Dictionary<int, LocationBandRate>();
            foreach (var band in LocationBandPricing.Bands)
            {
                if (ratesByKey.TryGetValue(band.RateKey, out var rate))
                {
                    bands[band.Index] = new LocationBandRate(
                        band.Index,
                        rate.MonthlyNetPence,
                        rate.AnnualNetPence
                    );
                }
            }

            return bands;
        }

        internal static string MinBasePlanForLocations(
            int locations,
            IReadOnlyList<PublicCatalogPlanSummaryDto> plans
        )
        {
            var paid = plans
                .Where(p =>
                    PaidPlanKeys.Contains(p.PlanKey, StringComparer.Ordinal)
                )
                .OrderBy(p => p.IncludedLocations)
                .ToList();

            foreach (var plan in paid)
            {
                if (plan.IncludedLocations >= locations)
                {
                    return plan.PlanKey;
                }
            }

            return "group";
        }

        internal static string? ParseBasePlanKey(string? value)
        {
            var key = value?.Trim().ToLowerInvariant();
            return key is "starter" or "growth" or "group" ? key : null;
        }

        private static string BasePlanToChosenPlan(string basePlan) =>
            basePlan switch
            {
                "starter" => BillingSubscriptionPlans.Starter,
                "growth" => BillingSubscriptionPlans.Growth,
                _ => BillingSubscriptionPlans.Group,
            };

        private static int PlanPeriodNetPence(
            PublicCatalogPlanSummaryDto plan,
            string cadence
        )
        {
            if (string.Equals(cadence, "annual", StringComparison.OrdinalIgnoreCase))
            {
                if (plan.AnnualNetPence > 0)
                {
                    return plan.AnnualNetPence;
                }

                return (int)Math.Round(
                    plan.MonthlyNetPence * 12 * CustomPlanPacks.AnnualFallbackFactor
                );
            }

            return plan.MonthlyNetPence;
        }

        private static List<PublicCatalogPlanSummaryDto> BuildPlanSummaries(
            CommercialCatalogVersion version
        )
        {
            return version.Plans
                .Where(p =>
                    PaidPlanKeys.Contains(
                        p.PlanKey.Trim().ToLowerInvariant(),
                        StringComparer.Ordinal
                    )
                )
                .Select(p => new PublicCatalogPlanSummaryDto
                {
                    PlanKey = p.PlanKey.Trim().ToLowerInvariant(),
                    DisplayName = p.DisplayName,
                    MonthlyNetPence = p.MonthlyNetPence,
                    AnnualNetPence = p.AnnualNetPence,
                    IncludedLocations = p.IncludedLocations,
                    IncludedTeamMembers = p.IncludedTeamMembers,
                    ActiveOffersAccount = p.ActiveOffersAccount,
                    ActiveQrPlacementsPerLocation = p.ActiveQrPlacementsPerLocation,
                    CreditsMonthlyAi = p.CreditsMonthlyAi ?? 0,
                    CreditsMonthlyEmail = p.CreditsMonthlyEmail ?? 0,
                    CreditsMonthlySms = p.CreditsMonthlySms ?? 0,
                })
                .ToList();
        }

        private static string PackTitle(string packKey) =>
            packKey switch
            {
                "team_users_5" => "Additional team users",
                "offers_5" => "Additional active Offers",
                "qr_10" => "Extra QR placements",
                "email_20k" => "+20,000 Email/month",
                "email_50k" => "+50,000 Email/month",
                "sms_500" => "+500 SMS/month",
                "sms_1000" => "+1,000 SMS/month",
                "sms_5000" => "+5,000 SMS/month",
                "ai_500" => "+500 AI actions/month",
                "ai_2000" => "+2,000 AI actions/month",
                _ => packKey,
            };
    }
}

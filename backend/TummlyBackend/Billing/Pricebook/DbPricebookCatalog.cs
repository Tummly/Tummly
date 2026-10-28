using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Billing.Pricebook
{
    /// <summary>
    /// Runtime pricebook from Admin-published commercial catalog versions.
    /// Falls back to JSON pack when DB has no published version (tests / pre-seed).
    /// </summary>
    public sealed class DbPricebookCatalog : IPricebookCatalog
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PricebookCatalog _jsonFallback;
        private readonly ConcurrentDictionary<string, PricebookSnapshot> _cache =
            new(StringComparer.Ordinal);

        private string? _currentPublicId;
        private readonly object _gate = new();

        public DbPricebookCatalog(
            IServiceScopeFactory scopeFactory,
            IHostEnvironment environment
        )
        {
            _scopeFactory = scopeFactory;
            _jsonFallback = PricebookCatalog.CreateForHost(environment);
        }

        public string CurrentPricebookId
        {
            get
            {
                EnsureCurrentLoaded();
                return _currentPublicId ?? _jsonFallback.CurrentPricebookId;
            }
        }

        public PricebookSnapshot GetRequired(string pricebookId)
        {
            if (string.IsNullOrWhiteSpace(pricebookId))
            {
                throw new InvalidOperationException("Pricebook id is required.");
            }

            if (_cache.TryGetValue(pricebookId, out var cached))
            {
                return cached;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var version = db.CommercialCatalogVersions
                .AsNoTracking()
                .Include(v => v.Plans)
                .Include(v => v.TopUpPacks)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates)
                .FirstOrDefault(v => v.PublicId == pricebookId);

            if (version != null)
            {
                var snapshot = CommercialCatalogMapper.ToSnapshot(version);
                _cache[pricebookId] = snapshot;
                return snapshot;
            }

            return _jsonFallback.GetRequired(pricebookId);
        }

        public BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available)
        {
            var book = GetRequired(CurrentPricebookId);
            var plans = new List<BillingCatalogPlanCardDto>();
            foreach (var key in new[] { "free", "pilot", "starter", "growth", "group" })
            {
                if (!book.Plans.TryGetValue(key, out var plan))
                {
                    continue;
                }

                var credits = plan.CreditsMonthly ?? plan.CreditsOneTime;
                plans.Add(
                    new BillingCatalogPlanCardDto
                    {
                        Id = plan.DisplayName,
                        MonthlyNetPence = plan.MonthlyNetPence,
                        AnnualNetPence = plan.AnnualNetPence,
                        IncludedLocations = plan.IncludedLocations,
                        IncludedEmail = credits?.Email ?? 0,
                        IncludedSms = credits?.Sms ?? 0,
                        IncludedAi = credits?.Ai ?? 0,
                        ExtraLocationNetPence =
                            key == "group"
                                ? book.ExtraGroupLocationMonthlyNetPence
                                : null,
                        ExtraLocationAnnualNetPence =
                            key == "group"
                                ? book.ExtraGroupLocationAnnualNetPence
                                : null,
                    }
                );
            }

            var packs = book.TopUpPacks
                .Where(pack =>
                    !(
                        pack.Channel == "sms"
                        && pack.Quantity == 5000
                        && !sms5000Available
                    )
                )
                .Select(pack => new BillingCatalogTopUpPackDto
                {
                    Channel = pack.Channel,
                    Quantity = pack.Quantity,
                    NetPence = pack.NetPence,
                })
                .ToList();

            return new BillingCurrentCatalogDto
            {
                PricebookId = book.Id,
                Plans = plans,
                TopUpPacks = packs,
                Sms5000Available = sms5000Available,
                VatRateBps = book.VatRateBps,
            };
        }

        public string FormatPlanPriceNet(PricebookPlan plan, string? billingCycle)
        {
            var pence =
                string.Equals(billingCycle, "Annual", StringComparison.OrdinalIgnoreCase)
                    ? plan.AnnualNetPence
                    : plan.MonthlyNetPence;
            return FormatPoundsFromPence(pence);
        }

        public string FormatIncludedCreditsLabel(PricebookPlan plan, string channel)
        {
            var once = plan.CreditsOneTime != null && plan.CreditsMonthly == null;
            var credits = plan.CreditsMonthly ?? plan.CreditsOneTime;
            if (credits == null)
            {
                return once ? "0 once" : "0 / month";
            }

            var qty = channel switch
            {
                "email" => credits.Email,
                "sms" => credits.Sms,
                "ai" => credits.Ai,
                _ => 0,
            };
            var formatted = qty.ToString("N0", CultureInfo.GetCultureInfo("en-GB"));
            return once ? $"{formatted} once" : $"{formatted} / month";
        }

        public void InvalidateCache()
        {
            lock (_gate)
            {
                _cache.Clear();
                _currentPublicId = null;
            }
        }

        private void EnsureCurrentLoaded()
        {
            if (_currentPublicId != null)
            {
                return;
            }

            lock (_gate)
            {
                if (_currentPublicId != null)
                {
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var published = db.CommercialCatalogVersions
                    .AsNoTracking()
                    .Where(v => v.Status == CommercialCatalogStatuses.Published)
                    .OrderByDescending(v => v.PublishedAtUtc)
                    .Select(v => v.PublicId)
                    .FirstOrDefault();

                _currentPublicId = published ?? _jsonFallback.CurrentPricebookId;
            }
        }

        private static string FormatPoundsFromPence(int pence)
        {
            if (pence % 100 == 0)
            {
                return $"£{pence / 100}";
            }

            return $"£{(pence / 100m).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"))}";
        }
    }
}

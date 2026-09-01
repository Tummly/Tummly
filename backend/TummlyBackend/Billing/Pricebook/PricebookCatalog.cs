using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Billing.Pricebook
{
    public sealed class PricebookCatalog : IPricebookCatalog
    {
        public const string CurrentIdFileName = "current-pricebook-id";
        public const string PackJsonFileName = "tummly_uk_billing_config_v3.0.json";
        public const string PackRelativeDirectory =
            "docs/product/billing-pack-v3.0";
        public const string AssetsRelativeDirectory = "Assets/billing-pack";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IReadOnlyDictionary<string, PricebookSnapshot> _byId;

        private PricebookCatalog(
            string currentPricebookId,
            IReadOnlyDictionary<string, PricebookSnapshot> byId
        )
        {
            CurrentPricebookId = currentPricebookId;
            _byId = byId;
        }

        public string CurrentPricebookId { get; }

        public static PricebookCatalog LoadFromDirectory(string packDirectory)
        {
            if (!Directory.Exists(packDirectory))
            {
                throw new InvalidOperationException(
                    $"Pricebook pack directory is missing: {packDirectory}"
                );
            }

            var currentIdPath = Path.Combine(packDirectory, CurrentIdFileName);
            if (!File.Exists(currentIdPath))
            {
                throw new InvalidOperationException(
                    $"Pricebook current-pricebook-id is missing: {currentIdPath}"
                );
            }

            var currentId = File.ReadAllText(currentIdPath).Trim();
            if (string.IsNullOrWhiteSpace(currentId))
            {
                throw new InvalidOperationException(
                    "Pricebook current-pricebook-id is empty."
                );
            }

            var jsonPath = Path.Combine(packDirectory, PackJsonFileName);
            if (!File.Exists(jsonPath))
            {
                throw new InvalidOperationException(
                    $"Pricebook tummly_uk_billing_config JSON is missing: {jsonPath}"
                );
            }

            using var stream = File.OpenRead(jsonPath);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            var snapshot = BindSnapshot(root);
            if (
                !string.Equals(
                    snapshot.Id,
                    currentId,
                    StringComparison.Ordinal
                )
            )
            {
                throw new InvalidOperationException(
                    $"Pricebook current id '{currentId}' does not match pack id '{snapshot.Id}'."
                );
            }

            return new PricebookCatalog(
                currentId,
                new Dictionary<string, PricebookSnapshot>(StringComparer.Ordinal)
                {
                    [snapshot.Id] = snapshot,
                }
            );
        }

        public static PricebookCatalog LoadFromContentRoot(string contentRootPath)
        {
            var packDirectory = ResolvePackDirectory(contentRootPath);
            return LoadFromDirectory(packDirectory);
        }

        public static string ResolvePackDirectory(string contentRootPath)
        {
            var candidates = new[]
            {
                Path.Combine(contentRootPath, AssetsRelativeDirectory),
                Path.GetFullPath(
                    Path.Combine(
                        contentRootPath,
                        "..",
                        "..",
                        PackRelativeDirectory
                    )
                ),
                Path.GetFullPath(
                    Path.Combine(contentRootPath, "..", PackRelativeDirectory)
                ),
            };

            foreach (var candidate in candidates)
            {
                if (
                    Directory.Exists(candidate)
                    && File.Exists(Path.Combine(candidate, CurrentIdFileName))
                    && File.Exists(Path.Combine(candidate, PackJsonFileName))
                )
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "Pricebook pack directory could not be resolved from content root."
            );
        }

        public PricebookSnapshot GetRequired(string pricebookId)
        {
            if (_byId.TryGetValue(pricebookId, out var snapshot))
            {
                return snapshot;
            }

            throw new InvalidOperationException(
                $"Pricebook '{pricebookId}' is not available."
            );
        }

        public BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available)
        {
            var book = GetRequired(CurrentPricebookId);
            var plans = new List<BillingCatalogPlanCardDto>();
            foreach (var key in new[] { "pilot", "starter", "growth", "group" })
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

        public static PricebookCatalog CreateForHost(IHostEnvironment environment)
        {
            return LoadFromContentRoot(environment.ContentRootPath);
        }

        private static string FormatPoundsFromPence(int pence)
        {
            if (pence % 100 == 0)
            {
                return $"£{pence / 100}";
            }

            return $"£{(pence / 100m).ToString("0.00", CultureInfo.GetCultureInfo("en-GB"))}";
        }

        private static PricebookSnapshot BindSnapshot(JsonElement root)
        {
            var pricebook = root.GetProperty("pricebook");
            var id = pricebook.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("pricebook.id is missing.");

            var vatRate = root.GetProperty("vat").GetProperty("rate").GetDecimal();
            var vatRateBps = (int)decimal.Round(vatRate * 10_000m);

            var plansElement = root.GetProperty("plans");
            var plans = new Dictionary<string, PricebookPlan>(StringComparer.Ordinal);
            foreach (var key in new[] { "pilot", "starter", "growth", "group" })
            {
                if (!plansElement.TryGetProperty(key, out var planElement))
                {
                    continue;
                }

                plans[key] = BindPlan(key, planElement);
            }

            int? extraMonthly = null;
            int? extraAnnual = null;
            PricebookChannelCredits? extraCreditsMonthly = null;
            if (
                plansElement.TryGetProperty(
                    "additional_group_location",
                    out var extra
                )
            )
            {
                extraMonthly = extra.GetProperty("monthly_price_pence").GetInt32();
                if (extra.TryGetProperty("annual_price_pence", out var annualExtra))
                {
                    extraAnnual = annualExtra.GetInt32();
                }

                if (extra.TryGetProperty("credits_monthly_added", out var extraCredits))
                {
                    extraCreditsMonthly = BindCredits(extraCredits);
                }
            }

            var topUps = BindTopUps(root.GetProperty("topups").GetProperty("packs"));

            return new PricebookSnapshot
            {
                Id = id,
                Plans = plans,
                TopUpPacks = topUps,
                VatRateBps = vatRateBps,
                ExtraGroupLocationMonthlyNetPence = extraMonthly,
                ExtraGroupLocationAnnualNetPence = extraAnnual,
                ExtraLocationCreditsMonthly = extraCreditsMonthly,
                InternalCostAssumptions = null,
                PaymentProvider = null,
            };
        }

        private static PricebookPlan BindPlan(string key, JsonElement element)
        {
            var displayName = char.ToUpperInvariant(key[0]) + key[1..];
            PricebookChannelCredits? oneTime = null;
            PricebookChannelCredits? monthly = null;
            if (element.TryGetProperty("credits_one_time", out var oneTimeElement))
            {
                oneTime = BindCredits(oneTimeElement);
            }

            if (element.TryGetProperty("credits_monthly", out var monthlyElement))
            {
                monthly = BindCredits(monthlyElement);
            }

            var monthlyPence = element.TryGetProperty("monthly_price_pence", out var monthlyPrice)
                ? monthlyPrice.GetInt32()
                : element.TryGetProperty("price_pence", out var price)
                    ? price.GetInt32()
                    : 0;
            var annualPence = element.TryGetProperty("annual_price_pence", out var annualPrice)
                ? annualPrice.GetInt32()
                : 0;
            var locations = element.TryGetProperty("locations", out var loc)
                ? loc.GetInt32()
                : 1;
            var users = element.TryGetProperty("users", out var userCount)
                ? userCount.GetInt32()
                : 0;
            var activeOffers = element.TryGetProperty(
                    "active_offers_account",
                    out var offers
                )
                ? offers.GetInt32()
                : 0;
            var activeQr = element
                .GetProperty("active_qr_placements_per_location")
                .GetInt32();
            var publishedGuestForms = element.TryGetProperty(
                    "published_guest_forms_per_location",
                    out var publishedForms
                )
                ? publishedForms.GetInt32()
                : element.TryGetProperty("published_guest_forms", out var publishedAlt)
                    ? publishedAlt.GetInt32()
                    : 1;
            var draftGuestForms = element.TryGetProperty(
                    "draft_guest_forms_per_location",
                    out var draftForms
                )
                ? draftForms.GetInt32()
                : element.TryGetProperty("draft_guest_forms", out var draftAlt)
                    ? draftAlt.GetInt32()
                    : 1;

            return new PricebookPlan
            {
                Key = key,
                DisplayName = displayName,
                MonthlyNetPence = monthlyPence,
                AnnualNetPence = annualPence,
                IncludedLocations = locations,
                IncludedTeamMembers = users,
                ActiveOffersAccount = activeOffers,
                CreditsOneTime = oneTime,
                CreditsMonthly = monthly,
                ActiveQrPlacementsPerLocation = activeQr,
                PublishedGuestFormsPerLocation = publishedGuestForms,
                DraftGuestFormsPerLocation = draftGuestForms,
            };
        }

        private static PricebookChannelCredits BindCredits(JsonElement element)
        {
            return new PricebookChannelCredits
            {
                Ai = element.GetProperty("ai").GetInt32(),
                Email = element.GetProperty("email").GetInt32(),
                Sms = element.GetProperty("sms").GetInt32(),
            };
        }

        private static List<PricebookTopUpPack> BindTopUps(JsonElement packs)
        {
            var list = new List<PricebookTopUpPack>();
            foreach (var channel in new[] { "ai", "email", "sms" })
            {
                if (!packs.TryGetProperty(channel, out var channelPacks))
                {
                    continue;
                }

                foreach (var pack in channelPacks.EnumerateArray())
                {
                    var lookupKey = pack.GetProperty("lookup_key").GetString()
                        ?? throw new InvalidOperationException(
                            $"topups.packs.{channel} lookup_key is missing."
                        );
                    list.Add(
                        new PricebookTopUpPack
                        {
                            Channel = channel,
                            Quantity = pack.GetProperty("quantity").GetInt32(),
                            NetPence = pack.GetProperty("price_pence").GetInt32(),
                            LookupKey = lookupKey,
                            ApprovalRequired =
                                pack.TryGetProperty("approval_required", out var approval)
                                && approval.GetBoolean(),
                        }
                    );
                }
            }

            return list;
        }
    }
}

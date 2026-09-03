using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopLocationRecommendationsService
        : IShopLocationRecommendationsService
    {
        private const int RecommendationWindowCalendarDays = 30;

        private static readonly HashSet<string> AllowedExistingMaterials =
            new(StringComparer.Ordinal)
            {
                "no",
                "yes",
                "not-sure",
            };

        private static readonly Dictionary<string, QrType> SkuToQrType =
            new(StringComparer.Ordinal)
            {
                ["table-tents"] = QrType.SmartGuest,
                ["counter-cards"] = QrType.CounterCard,
                ["window-stickers"] = QrType.WindowSticker,
                ["packaging-stickers"] = QrType.PackagingSticker,
                ["delivery-inserts"] = QrType.DeliveryInsert,
                ["receipt-stickers"] = QrType.ReceiptSticker,
            };

        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _engagement;
        private readonly IMaterialsCatalog _catalog;

        public ShopLocationRecommendationsService(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate engagement,
            IMaterialsCatalog catalog
        )
        {
            _context = context;
            _engagement = engagement;
            _catalog = catalog;
        }

        public async Task<ShopLocationDetailsBasisDto?> SaveDetailsAsync(
            int locationId,
            SaveShopLocationDetailsRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var exists = await _context.RestaurantLocations
                .AsNoTracking()
                .AnyAsync(row => row.Id == locationId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            var existingMaterials = NormalizeExistingMaterials(
                request.ExistingMaterials
            );
            var promptLocations = NormalizePromptLocations(
                request.PromptLocations
            );
            var takeawayVolume = NormalizeTakeawayVolume(request.TakeawayVolume);

            var row = await _context.ShopLocationDetails
                .FirstOrDefaultAsync(
                    row => row.LocationId == locationId,
                    cancellationToken
                );

            if (row == null)
            {
                row = new ShopLocationDetails { LocationId = locationId };
                _context.ShopLocationDetails.Add(row);
            }

            row.TableCount = Math.Max(0, request.TableCount);
            row.CounterCount = Math.Max(0, request.CounterCount);
            row.EntranceCount = Math.Max(0, request.EntranceCount);
            row.SecondaryEntranceCount = Math.Max(
                0,
                request.SecondaryEntranceCount
            );
            row.TakeawayVolume = takeawayVolume;
            row.PromptLocations = promptLocations;
            row.ExistingMaterials = existingMaterials;
            row.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return MapBasis(row);
        }

        public async Task<ShopLocationRecommendationsDto> GetRecommendationsAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var (fromDate, toDate, fromUtc, toUtc) = ResolveRecommendationWindow();

            var details = await _context.ShopLocationDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.LocationId == locationId,
                    cancellationToken
                );

            var response = new ShopLocationRecommendationsDto
            {
                LocationId = locationId,
                Window = new ShopRecommendationsWindowDto
                {
                    From = fromDate.ToString("yyyy-MM-dd"),
                    To = toDate.ToString("yyyy-MM-dd"),
                },
            };

            if (details == null)
            {
                response.NeedsLocationDetails = true;
                response.Lines = Array.Empty<ShopRecommendationLineDto>();
                response.Summary = EmptySummary();
                return response;
            }

            response.NeedsLocationDetails = false;
            response.BasedOn = MapBasis(details);

            var prompts = ParsePromptSet(details.PromptLocations);
            var activityByQrType = await LoadActivityByQrTypeAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );
            var hasActivity = activityByQrType.Values.Any(
                row => row.FeedbackSubmitted > 0 || row.QrScans > 0
            );

            var lines = BuildRecommendationLines(
                details,
                prompts,
                activityByQrType,
                hasActivity
            );

            response.Lines = lines;
            response.Summary = BuildSummary(lines);
            return response;
        }

        private IReadOnlyList<ShopRecommendationLineDto> BuildRecommendationLines(
            ShopLocationDetails details,
            HashSet<string> prompts,
            IReadOnlyDictionary<QrType, QrTypeActivity> activityByQrType,
            bool hasActivity
        )
        {
            var includedSkus = new List<(string SkuId, int Quantity, string AllocationText, string Reason)>();

            foreach (var (skuId, qrType) in SkuToQrType)
            {
                if (skuId == "receipt-stickers" && !prompts.Contains("receipts"))
                {
                    continue;
                }

                var inPrompts = IsSkuInPromptBaseline(skuId, prompts);
                activityByQrType.TryGetValue(qrType, out var activity);
                var feedbackCount = activity?.FeedbackSubmitted ?? 0;
                var activityAdd = !inPrompts && feedbackCount >= 5;

                if (skuId == "receipt-stickers")
                {
                    if (!inPrompts)
                    {
                        continue;
                    }
                }
                else if (!inPrompts && !activityAdd)
                {
                    continue;
                }

                var baseline = ComputeBaselineQty(skuId, details, inPrompts || skuId == "receipt-stickers");
                var quantity = ApplyActivityFloor(
                    skuId,
                    baseline,
                    feedbackCount
                );

                if (quantity <= 0)
                {
                    continue;
                }

                var fromPrompts = inPrompts || skuId == "receipt-stickers";
                var allocationText = BuildAllocationText(
                    skuId,
                    details,
                    quantity,
                    baseline,
                    fromPrompts
                );
                var reason = BuildReason(
                    skuId,
                    details,
                    qrType,
                    feedbackCount,
                    hasActivity,
                    fromPrompts
                );

                includedSkus.Add((skuId, quantity, allocationText, reason));
            }

            var lines = new List<ShopRecommendationLineDto>();
            foreach (var row in includedSkus)
            {
                var catalog = _catalog.TryBuildDetail(row.SkuId);
                if (catalog == null)
                {
                    continue;
                }

                lines.Add(new ShopRecommendationLineDto
                {
                    SkuId = row.SkuId,
                    Quantity = row.Quantity,
                    Title = catalog.Title,
                    UnitNetPence = catalog.UnitNetPence,
                    ImageUrl = catalog.ImageUrl,
                    AllocationText = row.AllocationText,
                    Reason = row.Reason,
                });
            }

            return lines;
        }

        private static bool IsSkuInPromptBaseline(string skuId, HashSet<string> prompts)
        {
            return skuId switch
            {
                "table-tents" => prompts.Contains("tables"),
                "counter-cards" => prompts.Contains("counters")
                    || prompts.Contains("collection"),
                "window-stickers" => prompts.Contains("windows"),
                "packaging-stickers" => prompts.Contains("packaging"),
                "delivery-inserts" => prompts.Contains("delivery"),
                "receipt-stickers" => prompts.Contains("receipts"),
                _ => false,
            };
        }

        private static int ComputeBaselineQty(
            string skuId,
            ShopLocationDetails details,
            bool fromPrompts
        )
        {
            return skuId switch
            {
                "table-tents" => fromPrompts
                    ? details.TableCount + 2
                    : 12,
                "counter-cards" => fromPrompts
                    ? details.CounterCount + 1
                    : 2,
                "window-stickers" => fromPrompts
                    ? details.EntranceCount
                        + details.SecondaryEntranceCount
                        + 1
                    : 2,
                "packaging-stickers" or "delivery-inserts" =>
                    TakeawayPackQty(details.TakeawayVolume),
                "receipt-stickers" => 1,
                _ => 0,
            };
        }

        private static int ApplyActivityFloor(
            string skuId,
            int baseline,
            int feedbackSubmitted
        )
        {
            if (skuId == "receipt-stickers")
            {
                return baseline;
            }

            if (feedbackSubmitted < 5)
            {
                return baseline;
            }

            var activityFloor = (int)Math.Ceiling(baseline * 1.1) + 2;
            var cap = baseline * 2;
            activityFloor = Math.Min(activityFloor, cap);
            return Math.Max(baseline, activityFloor);
        }

        private static int TakeawayPackQty(string takeawayVolume)
        {
            return takeawayVolume switch
            {
                "fewer-than-100" => 25,
                "100-249" => 50,
                "250-499" => 100,
                "500-999" => 150,
                "1000-plus" => 200,
                _ => 50,
            };
        }

        private static string BuildAllocationText(
            string skuId,
            ShopLocationDetails details,
            int quantity,
            int baseline,
            bool fromPrompts
        )
        {
            return skuId switch
            {
                "table-tents" when fromPrompts =>
                    $"{details.TableCount} for guest tables + {Math.Max(0, quantity - details.TableCount)} spare",
                "counter-cards" when fromPrompts =>
                    $"{details.CounterCount} for counters + {Math.Max(0, quantity - details.CounterCount)} spare",
                "window-stickers" when fromPrompts =>
                    $"{details.EntranceCount + details.SecondaryEntranceCount} for entrances + {Math.Max(0, quantity - (details.EntranceCount + details.SecondaryEntranceCount))} spare",
                "packaging-stickers" or "delivery-inserts" =>
                    $"{quantity} for weekly takeaway volume",
                "receipt-stickers" => "1 roll for printed receipts",
                _ => $"{quantity} recommended",
            };
        }

        private static string BuildReason(
            string skuId,
            ShopLocationDetails details,
            QrType qrType,
            int feedbackSubmitted,
            bool hasActivity,
            bool fromPrompts
        )
        {
            var windowLabel =
                $"the last {RecommendationWindowCalendarDays} days";
            var qrLabel = QrTypeLabel(qrType);
            var citeActivity = hasActivity && feedbackSubmitted >= 5;

            // Activity-only adds (not in promptLocations) cite window feedback,
            // not placement counts that did not put the SKU in the kit.
            if (!fromPrompts)
            {
                return citeActivity
                    ? $"Based on {qrLabel} feedback in {windowLabel}."
                    : "Based on how this location operates.";
            }

            if (!citeActivity)
            {
                return skuId switch
                {
                    "table-tents" =>
                        $"Based on {details.TableCount} guest tables and how this location operates.",
                    "counter-cards" =>
                        $"Based on {details.CounterCount} service counters and how this location operates.",
                    "window-stickers" =>
                        $"Based on {details.EntranceCount + details.SecondaryEntranceCount} entrances and how this location operates.",
                    "packaging-stickers" =>
                        "Based on takeaway volume and how this location operates.",
                    "delivery-inserts" =>
                        "Based on delivery volume and how this location operates.",
                    "receipt-stickers" =>
                        "Based on receipt prompts and how this location operates.",
                    _ => "Based on how this location operates.",
                };
            }

            return skuId switch
            {
                "table-tents" =>
                    $"Based on {details.TableCount} guest tables and {qrLabel} feedback in {windowLabel}.",
                "counter-cards" =>
                    $"Based on {details.CounterCount} service counters and {qrLabel} feedback in {windowLabel}.",
                "window-stickers" =>
                    $"Based on {details.EntranceCount + details.SecondaryEntranceCount} entrances and {qrLabel} feedback in {windowLabel}.",
                "packaging-stickers" or "delivery-inserts" =>
                    $"Based on takeaway activity and {qrLabel} feedback in {windowLabel}.",
                _ =>
                    $"Based on {qrLabel} feedback in {windowLabel}.",
            };
        }

        private static string QrTypeLabel(QrType qrType) =>
            qrType switch
            {
                QrType.SmartGuest => "Smart Guest",
                QrType.CounterCard => "counter card",
                QrType.WindowSticker => "window sticker",
                QrType.PackagingSticker => "packaging sticker",
                QrType.DeliveryInsert => "delivery insert",
                QrType.ReceiptSticker => "receipt sticker",
                _ => qrType.ToString(),
            };

        private async Task<
            Dictionary<QrType, QrTypeActivity>
        > LoadActivityByQrTypeAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var qrCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                    && q.QrType != QrType.DigitalGuestLink
                )
                .Select(q => new { q.Id, q.QrType })
                .ToListAsync(cancellationToken);

            if (qrCodes.Count == 0)
            {
                return new Dictionary<QrType, QrTypeActivity>();
            }

            var qrCodeIds = qrCodes.Select(q => q.Id).ToList();
            var scans = await _engagement.GroupScansByQrCodeAsync(
                qrCodeIds,
                fromUtc,
                toUtc
            );
            var feedback = await _engagement.GroupFeedbackByQrCodeAsync(
                qrCodeIds,
                fromUtc,
                toUtc
            );

            var scanLookup = scans.ToDictionary(x => x.QrCodeId, x => x.Count);
            var feedbackLookup = feedback.ToDictionary(
                x => x.QrCodeId,
                x => x.FeedbackSubmitted
            );

            var rollup = new Dictionary<QrType, QrTypeActivity>();
            foreach (var qr in qrCodes)
            {
                if (!rollup.TryGetValue(qr.QrType, out var bucket))
                {
                    bucket = new QrTypeActivity();
                    rollup[qr.QrType] = bucket;
                }

                bucket.QrScans += scanLookup.GetValueOrDefault(qr.Id);
                bucket.FeedbackSubmitted += feedbackLookup.GetValueOrDefault(
                    qr.Id
                );
            }

            return rollup;
        }

        private static (
            DateTime FromDate,
            DateTime ToDate,
            DateTime FromUtc,
            DateTime ToUtc
        ) ResolveRecommendationWindow()
        {
            var toDate = DateTime.UtcNow.Date;
            var fromDate = toDate.AddDays(-RecommendationWindowCalendarDays);
            var fromUtc = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(
                toDate.AddDays(1),
                DateTimeKind.Utc
            );
            return (fromDate, toDate, fromUtc, toUtc);
        }

        private static ShopLocationDetailsBasisDto MapBasis(
            ShopLocationDetails row
        )
        {
            return new ShopLocationDetailsBasisDto
            {
                TableCount = row.TableCount,
                CounterCount = row.CounterCount,
                EntranceCount = row.EntranceCount,
                SecondaryEntranceCount = row.SecondaryEntranceCount,
                TakeawayVolume = row.TakeawayVolume,
                PromptLocations = ParsePromptList(row.PromptLocations),
                ExistingMaterials = row.ExistingMaterials,
            };
        }

        private static HashSet<string> ParsePromptSet(string promptLocations)
        {
            return ParsePromptList(promptLocations)
                .ToHashSet(StringComparer.Ordinal);
        }

        private static IReadOnlyList<string> ParsePromptList(string promptLocations)
        {
            if (string.IsNullOrWhiteSpace(promptLocations))
            {
                return Array.Empty<string>();
            }

            return promptLocations
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static string NormalizePromptLocations(string promptLocations)
        {
            return string.Join(
                ",",
                ParsePromptList(promptLocations)
            );
        }

        private static string NormalizeExistingMaterials(string value)
        {
            var normalized = (value ?? "no").Trim().ToLowerInvariant();
            return AllowedExistingMaterials.Contains(normalized)
                ? normalized
                : "no";
        }

        private static string NormalizeTakeawayVolume(string value)
        {
            var normalized = (value ?? "not-sure").Trim().ToLowerInvariant();
            return normalized switch
            {
                "fewer-than-100" => normalized,
                "100-249" => normalized,
                "250-499" => normalized,
                "500-999" => normalized,
                "1000-plus" => normalized,
                "not-sure" => normalized,
                _ => "not-sure",
            };
        }

        private static ShopRecommendationsSummaryDto BuildSummary(
            IReadOnlyList<ShopRecommendationLineDto> lines
        )
        {
            return new ShopRecommendationsSummaryDto
            {
                MaterialTypeCount = lines.Count,
                TotalPieces = lines.Sum(row => row.Quantity),
                MaterialsNetPence = lines.Sum(
                    row => row.UnitNetPence * row.Quantity
                ),
                Currency = "GBP",
            };
        }

        private static ShopRecommendationsSummaryDto EmptySummary()
        {
            return new ShopRecommendationsSummaryDto
            {
                MaterialTypeCount = 0,
                TotalPieces = 0,
                MaterialsNetPence = 0,
                Currency = "GBP",
            };
        }

        private sealed class QrTypeActivity
        {
            public int QrScans { get; set; }

            public int FeedbackSubmitted { get; set; }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaign recommendation — server-loaded metrics, allow-list AI, 30 min cache.
    /// </summary>
    public sealed class CampaignRecommendationService
        : ICampaignRecommendationService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        private const int NewGuestDays = 13;
        private const int DormantDays = 90;

        private readonly ApplicationDbContext _context;
        private readonly ICampaignRecommendationProvider _provider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CampaignRecommendationService> _logger;

        public CampaignRecommendationService(
            ApplicationDbContext context,
            ICampaignRecommendationProvider provider,
            IMemoryCache cache,
            ILogger<CampaignRecommendationService> logger
        )
        {
            _context = context;
            _provider = provider;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CampaignRecommendationServiceResult> RecommendAsync(
            int operatorUserId,
            CampaignRecommendationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.LocationId <= 0)
            {
                throw new ArgumentException("locationId is required.");
            }

            var preset = NormalizePreset(request.OverviewDatePreset);
            ValidateWindow(preset, request.From, request.To);

            var locationName = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == request.LocationId)
                .Select(location => location.LocationName)
                .FirstOrDefaultAsync(cancellationToken)
                ?? string.Empty;

            var metrics = await LoadMetricsAsync(
                request.LocationId,
                preset,
                request.From,
                request.To,
                cancellationToken
            );

            var cacheKey = BuildCacheKey(
                operatorUserId,
                request.LocationId,
                preset,
                request.From,
                request.To
            );

            if (!request.Refresh
                && _cache.TryGetValue(cacheKey, out CampaignRecommendationDto? cached)
                && cached is not null)
            {
                return new CampaignRecommendationServiceResult.Ok(cached);
            }

            if (ShouldSkipAi(metrics))
            {
                var noneDto = BuildNoneDto(locationName, metrics);
                CacheRecommendation(cacheKey, noneDto);
                return new CampaignRecommendationServiceResult.Ok(noneDto);
            }

            CampaignRecommendationProviderResult providerResult;
            try
            {
                providerResult = await _provider.RecommendAsync(
                    new CampaignRecommendationProviderInput(
                        LocationName: locationName,
                        OverviewDatePreset: preset,
                        FromUtc: request.From,
                        ToUtc: request.To,
                        Metrics: metrics
                    ),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Campaign recommendation provider threw for location {LocationId}",
                    request.LocationId
                );
                return new CampaignRecommendationServiceResult.Failed(
                    "Could not load a campaign recommendation. Please try again.",
                    Retryable: true
                );
            }

            if (providerResult is CampaignRecommendationProviderResult.Failed failed)
            {
                return new CampaignRecommendationServiceResult.Failed(
                    "Could not load a campaign recommendation. Please try again.",
                    failed.Retryable
                );
            }

            if (
                providerResult
                is not CampaignRecommendationProviderResult.Succeeded succeeded
            )
            {
                return new CampaignRecommendationServiceResult.Failed(
                    "Could not load a campaign recommendation. Please try again.",
                    Retryable: true
                );
            }

            if (!TryMapSucceeded(
                    succeeded.Output,
                    locationName,
                    metrics,
                    out var dto,
                    out var mapError
                ))
            {
                _logger.LogWarning(
                    "Campaign recommendation output rejected: {Reason}",
                    mapError
                );
                return new CampaignRecommendationServiceResult.Failed(
                    "Could not load a campaign recommendation. Please try again.",
                    Retryable: true
                );
            }

            CacheRecommendation(cacheKey, dto);
            return new CampaignRecommendationServiceResult.Ok(dto);
        }

        private async Task<CampaignRecommendationMetrics> LoadMetricsAsync(
            int locationId,
            string preset,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken
        )
        {
            var utcNow = DateTime.UtcNow;
            var newGuestCutoff = utcNow.AddDays(-NewGuestDays);
            var dormantCutoff = utcNow.AddDays(-DormantDays);

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking(),
                [locationId]
            );

            var overviewQuery = scoped;
            if (!string.Equals(preset, "all-time", StringComparison.Ordinal)
                && fromUtc is not null
                && toUtc is not null)
            {
                overviewQuery = GuestsListQueryComposer.ApplyCapturedAtWindow(
                    overviewQuery,
                    fromUtc.Value,
                    toUtc.Value
                );
            }

            var marketingEligible = await GuestsListQueryComposer
                .WhereMarketingEligible(overviewQuery)
                .CountAsync(cancellationToken);

            return new CampaignRecommendationMetrics(
                MarketingEligible: marketingEligible,
                AllGuests: await scoped.CountAsync(cancellationToken),
                NewGuests: await GuestsListQueryComposer
                    .WhereNewGuest(scoped, newGuestCutoff)
                    .CountAsync(cancellationToken),
                NeedsRecovery: await GuestsListQueryComposer
                    .WhereNeedsRecovery(scoped)
                    .CountAsync(cancellationToken),
                PositiveFeedback: await GuestsListQueryComposer
                    .WherePositiveFeedback(scoped)
                    .CountAsync(cancellationToken),
                DormantGuests: await GuestsListQueryComposer
                    .WhereDormant(scoped, dormantCutoff)
                    .CountAsync(cancellationToken)
            );
        }

        /// <summary>
        /// Pre-AI none gate: skip Azure when marketing eligible is 0 and no
        /// supporting live Smart Group count for Thank / Re-engage / Recovery.
        /// </summary>
        private static bool ShouldSkipAi(CampaignRecommendationMetrics metrics)
        {
            if (metrics.MarketingEligible > 0)
            {
                return false;
            }

            var thankSupport =
                metrics.NewGuests > 0 || metrics.PositiveFeedback > 0;
            var reEngageSupport = metrics.DormantGuests > 0;
            var recoverySupport = metrics.NeedsRecovery > 0;

            return !thankSupport && !reEngageSupport && !recoverySupport;
        }

        private static bool TryMapSucceeded(
            CampaignRecommendationModelOutput output,
            string locationName,
            CampaignRecommendationMetrics metrics,
            out CampaignRecommendationDto dto,
            out string? error
        )
        {
            dto = new CampaignRecommendationDto { Type = "none" };
            error = null;

            if (!CampaignRecommendationStructuredOutput.IsAllowedType(output.Type))
            {
                error = $"Disallowed type '{output.Type}'.";
                return false;
            }

            if (string.Equals(output.Type, "none", StringComparison.Ordinal))
            {
                dto = BuildNoneDto(locationName, metrics);
                return true;
            }

            if (
                string.IsNullOrWhiteSpace(output.Title)
                || string.IsNullOrWhiteSpace(output.Opportunity)
                || string.IsNullOrWhiteSpace(output.EligibleAudience)
                || output.WhyBullets is null
                || output.WhyBullets.Count == 0
                || string.IsNullOrWhiteSpace(output.SuggestedChannel)
                || output.DraftPrefill is null
            )
            {
                error = "Non-none recommendation is missing required fields.";
                return false;
            }

            if (output.SuggestedChannel is not ("email" or "sms"))
            {
                error = "Invalid suggested channel.";
                return false;
            }

            if (!TryValidateDraftPrefill(output.DraftPrefill, out error))
            {
                return false;
            }

            dto = new CampaignRecommendationDto
            {
                Type = output.Type,
                Title = output.Title,
                Opportunity = output.Opportunity,
                EligibleAudience = output.EligibleAudience,
                WhyBullets = output.WhyBullets.ToArray(),
                SuggestedChannel = output.SuggestedChannel,
                EstimatedUsage = output.EstimatedUsage,
                EchoedCounts = ToEchoedCounts(metrics),
                DraftPrefill = new CampaignRecommendationDraftPrefillDto
                {
                    GoalId = output.DraftPrefill.GoalId,
                    AudienceKey = output.DraftPrefill.AudienceKey,
                    Channel = output.DraftPrefill.Channel,
                    OfferStance = output.DraftPrefill.OfferStance,
                    CampaignName = output.DraftPrefill.CampaignName,
                    MessageSubject = output.DraftPrefill.MessageSubject,
                    MessageBody = output.DraftPrefill.MessageBody,
                },
                LocationName = locationName,
            };
            return true;
        }

        private static bool TryValidateDraftPrefill(
            CampaignRecommendationDraftPrefillOutput prefill,
            out string? error
        )
        {
            error = null;

            if (!CampaignProductAllowLists.IsAllowedGoalId(prefill.GoalId))
            {
                error = $"Disallowed draftPrefill goalId '{prefill.GoalId}'.";
                return false;
            }

            if (!CampaignProductAllowLists.IsAllowedAudienceKey(prefill.AudienceKey))
            {
                error =
                    $"Disallowed draftPrefill audienceKey '{prefill.AudienceKey}'.";
                return false;
            }

            if (!CampaignProductAllowLists.IsAllowedChannel(prefill.Channel))
            {
                error = $"Disallowed draftPrefill channel '{prefill.Channel}'.";
                return false;
            }

            if (!CampaignProductAllowLists.IsAllowedOfferStance(prefill.OfferStance))
            {
                error =
                    $"Disallowed draftPrefill offerStance '{prefill.OfferStance}'.";
                return false;
            }

            return true;
        }

        private static CampaignRecommendationDto BuildNoneDto(
            string locationName,
            CampaignRecommendationMetrics metrics
        )
        {
            return new CampaignRecommendationDto
            {
                Type = "none",
                EchoedCounts = ToEchoedCounts(metrics),
                LocationName = locationName,
            };
        }

        private static CampaignRecommendationEchoedCountsDto ToEchoedCounts(
            CampaignRecommendationMetrics metrics
        )
        {
            return new CampaignRecommendationEchoedCountsDto
            {
                MarketingEligible = metrics.MarketingEligible,
                AllGuests = metrics.AllGuests,
                NewGuests = metrics.NewGuests,
                NeedsRecovery = metrics.NeedsRecovery,
                PositiveFeedback = metrics.PositiveFeedback,
                DormantGuests = metrics.DormantGuests,
            };
        }

        private void CacheRecommendation(
            string cacheKey,
            CampaignRecommendationDto dto
        )
        {
            _cache.Set(
                cacheKey,
                dto,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl,
                }
            );
        }

        private static string BuildCacheKey(
            int operatorUserId,
            int locationId,
            string preset,
            DateTime? fromUtc,
            DateTime? toUtc
        )
        {
            var fromToken = fromUtc?.ToUniversalTime().ToString("O") ?? "null";
            var toToken = toUtc?.ToUniversalTime().ToString("O") ?? "null";
            return $"campaign-recommendation:{operatorUserId}:{locationId}:{preset}:{fromToken}:{toToken}";
        }

        private static string NormalizePreset(string? preset)
        {
            var key = (preset ?? "last30").Trim().ToLowerInvariant();
            return key.Length == 0 ? "last30" : key;
        }

        private static void ValidateWindow(
            string preset,
            DateTime? fromUtc,
            DateTime? toUtc
        )
        {
            if (string.Equals(preset, "all-time", StringComparison.Ordinal))
            {
                if (fromUtc is not null || toUtc is not null)
                {
                    throw new ArgumentException(
                        "all-time overview must omit from and to."
                    );
                }

                return;
            }

            if (fromUtc is null || toUtc is null)
            {
                throw new ArgumentException(
                    "from and to are required for a bounded overview window."
                );
            }

            if (fromUtc > toUtc)
            {
                throw new ArgumentException("from must be on or before to.");
            }
        }
    }
}

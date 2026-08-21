using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TummlyBackend.Data;
using TummlyBackend.DTOs.OperatorHome;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Home recommendation orchestrator — ownership is at the controller;
    /// this service loads metrics, routes type, caches, and fills Home-native copy.
    /// Campaign allow-list types hand off via <see cref="CompleteCampaignRecommendationAsync"/>
    /// (ticket 06 wires CampaignRecommendationService). Free call — no credit debit.
    /// </summary>
    public sealed class HomeRecommendationService : IHomeRecommendationService
    {
        private const int NewGuestDays = 13;
        private const int DormantDays = 90;

        private static readonly string FailMessage =
            "Could not load a home recommendation. Please try again.";

        private readonly ApplicationDbContext _context;
        private readonly IHomeRecommendationProvider _provider;
        private readonly IDistributedCache _cache;
        private readonly ILogger<HomeRecommendationService> _logger;

        public HomeRecommendationService(
            ApplicationDbContext context,
            IHomeRecommendationProvider provider,
            IDistributedCache cache,
            ILogger<HomeRecommendationService> logger
        )
        {
            _context = context;
            _provider = provider;
            _cache = cache;
            _logger = logger;
        }

        public async Task<HomeRecommendationServiceResult> RecommendAsync(
            int operatorUserId,
            HomeRecommendationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.LocationId <= 0)
            {
                throw new ArgumentException("locationId is required.");
            }

            var preset = HomeRecommendationContract.NormalizePreset(
                request.OverviewDatePreset
            );
            HomeRecommendationContract.EnsureResolvedWindow(
                preset,
                request.From,
                request.To
            );

            var fromUtc = EnsureUtc(request.From!.Value);
            var toUtc = EnsureUtc(request.To!.Value);

            var cacheKey = HomeRecommendationContract.BuildCacheKey(
                operatorUserId,
                request.LocationId,
                preset,
                fromUtc,
                toUtc
            );

            if (!request.Refresh)
            {
                var cached = await TryGetCachedRecommendationAsync(
                    cacheKey,
                    cancellationToken
                );
                if (cached is not null)
                {
                    return new HomeRecommendationServiceResult.Ok(cached);
                }
            }

            var locationName = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(location => location.Id == request.LocationId)
                .Select(location => location.LocationName)
                .FirstOrDefaultAsync(cancellationToken)
                ?? string.Empty;

            var metrics = await LoadMetricsAsync(
                request.LocationId,
                fromUtc,
                toUtc,
                cancellationToken
            );

            var selectedType = HomeRecommendationDomainRouter.SelectType(metrics);

            if (string.Equals(selectedType, "none", StringComparison.Ordinal))
            {
                var noneDto = BuildNoneDto(locationName);
                await CacheRecommendationAsync(cacheKey, noneDto, cancellationToken);
                return new HomeRecommendationServiceResult.Ok(noneDto);
            }

            if (HomeRecommendationContract.IsCampaignType(selectedType))
            {
                // Seam for ticket 06 — CampaignRecommendationService handoff.
                return await CompleteCampaignRecommendationAsync(
                    operatorUserId,
                    request,
                    selectedType,
                    metrics,
                    locationName,
                    cacheKey,
                    cancellationToken
                );
            }

            HomeRecommendationProviderResult providerResult;
            try
            {
                providerResult = await _provider.RecommendAsync(
                    new HomeRecommendationProviderInput(
                        SelectedType: selectedType,
                        LocationName: locationName,
                        OverviewDatePreset: preset,
                        FromUtc: fromUtc,
                        ToUtc: toUtc,
                        Metrics: metrics
                    ),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Home recommendation provider threw for location {LocationId}",
                    request.LocationId
                );
                return new HomeRecommendationServiceResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            if (providerResult is HomeRecommendationProviderResult.Failed failed)
            {
                return new HomeRecommendationServiceResult.Failed(
                    FailMessage,
                    failed.Retryable
                );
            }

            if (
                providerResult
                is not HomeRecommendationProviderResult.Succeeded succeeded
            )
            {
                return new HomeRecommendationServiceResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            if (!TryMapSucceeded(
                    succeeded.Output,
                    selectedType,
                    locationName,
                    out var dto,
                    out var mapError
                ))
            {
                _logger.LogWarning(
                    "Home recommendation output rejected: {Reason}",
                    mapError
                );
                return new HomeRecommendationServiceResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            await CacheRecommendationAsync(cacheKey, dto, cancellationToken);
            return new HomeRecommendationServiceResult.Ok(dto);
        }

        /// <summary>
        /// Ticket 06 replaces this with CampaignRecommendationService reuse.
        /// Until then campaign-type selection fails retryably (no invented campaign copy).
        /// </summary>
        private Task<HomeRecommendationServiceResult> CompleteCampaignRecommendationAsync(
            int operatorUserId,
            HomeRecommendationRequest request,
            string selectedType,
            HomeRecommendationMetrics metrics,
            string locationName,
            string cacheKey,
            CancellationToken cancellationToken
        )
        {
            _ = (operatorUserId, request, selectedType, metrics, locationName, cacheKey, cancellationToken);
            _logger.LogInformation(
                "Home campaign-type handoff not wired yet (type {Type}); ticket 06.",
                selectedType
            );
            return Task.FromResult<HomeRecommendationServiceResult>(
                new HomeRecommendationServiceResult.Failed(
                    FailMessage,
                    Retryable: true
                )
            );
        }

        private async Task<HomeRecommendationMetrics> LoadMetricsAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var utcNow = DateTime.UtcNow;
            var newGuestCutoff = utcNow.AddDays(-NewGuestDays);
            var dormantCutoff = utcNow.AddDays(-DormantDays);

            var feedbackInWindow = _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                );

            var openFeedbackCount = await feedbackInWindow.CountAsync(
                f =>
                    f.WorkflowStatus == FeedbackWorkflowStatus.New
                    || f.WorkflowStatus == FeedbackWorkflowStatus.InProgress,
                cancellationToken
            );

            var needsAttentionCount = await feedbackInWindow.CountAsync(
                f =>
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment == FeedbackSentiment.Negative
                    && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved,
                cancellationToken
            );

            var guestsJoinedInWindow = await _context.LocationGuests
                .AsNoTracking()
                .CountAsync(
                    lg =>
                        lg.RestaurantLocationId == locationId
                        && lg.CreatedAt >= fromUtc
                        && lg.CreatedAt < toUtc,
                    cancellationToken
                );

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking(),
                [locationId]
            );

            var marketingEligible = await GuestsListQueryComposer
                .WhereMarketingEligible(scoped)
                .CountAsync(cancellationToken);

            var activeOffers = await _context.CatalogOffers
                .AsNoTracking()
                .CountAsync(
                    o =>
                        o.RestaurantLocationId == locationId
                        && o.Status == CatalogOfferStatus.Active,
                    cancellationToken
                );

            var venueToday = CatalogOfferStatus.VenueLocalToday(utcNow, 0);
            var offerRows = await _context.CatalogOffers
                .AsNoTracking()
                .Where(o => o.RestaurantLocationId == locationId)
                .Select(o => new
                {
                    o.Status,
                    o.Validity,
                    o.CustomExpiryDate,
                })
                .ToListAsync(cancellationToken);

            var offerNeedsAttentionCount = offerRows.Count(row =>
            {
                var effective = CatalogOfferStatus.ResolveEffectiveStatus(
                    row.Status,
                    row.Validity,
                    row.CustomExpiryDate,
                    venueToday
                );
                return CatalogOfferStatus.IsNeedsAttentionRule(
                    row.Validity,
                    row.CustomExpiryDate,
                    effective,
                    venueToday
                );
            });

            // HasNoActiveOffers stays false unless a future confirmation rule
            // flips it — zero Active alone must not force promote-or-fix-offer
            // (router Empty bag uses ActiveOffers: 0 + HasNoActiveOffers: false).
            return new HomeRecommendationMetrics(
                OpenFeedbackCount: openFeedbackCount,
                NeedsAttentionCount: needsAttentionCount,
                GuestsJoinedInWindow: guestsJoinedInWindow,
                MarketingEligible: marketingEligible,
                ActiveOffers: activeOffers,
                HasNoActiveOffers: false,
                OfferNeedsAttentionCount: offerNeedsAttentionCount,
                NewGuests: await GuestsListQueryComposer
                    .WhereNewGuest(scoped, newGuestCutoff)
                    .CountAsync(cancellationToken),
                PositiveFeedback: await GuestsListQueryComposer
                    .WherePositiveFeedback(scoped)
                    .CountAsync(cancellationToken),
                DormantGuests: await GuestsListQueryComposer
                    .WhereDormant(scoped, dormantCutoff)
                    .CountAsync(cancellationToken),
                NeedsRecovery: await GuestsListQueryComposer
                    .WhereNeedsRecovery(scoped)
                    .CountAsync(cancellationToken)
            );
        }

        private static bool TryMapSucceeded(
            HomeRecommendationModelOutput output,
            string selectedType,
            string locationName,
            out HomeRecommendationDto dto,
            out string? error
        )
        {
            dto = new HomeRecommendationDto { Type = "none" };
            error = null;

            if (!HomeRecommendationStructuredOutput.IsAllowedNativeType(output.Type))
            {
                error = $"Disallowed type '{output.Type}'.";
                return false;
            }

            if (string.Equals(output.Type, "none", StringComparison.Ordinal))
            {
                dto = BuildNoneDto(locationName);
                return true;
            }

            if (!string.Equals(output.Type, selectedType, StringComparison.Ordinal))
            {
                error =
                    $"Provider type '{output.Type}' does not match selected '{selectedType}'.";
                return false;
            }

            if (
                string.IsNullOrWhiteSpace(output.Title)
                || string.IsNullOrWhiteSpace(output.Opportunity)
                || output.WhyBullets is null
                || output.WhyBullets.Count == 0
                || output.Action is null
            )
            {
                error = "Non-none recommendation is missing required fields.";
                return false;
            }

            if (!HomeRecommendationContract.IsAllowedDomainActionKind(output.Action.Kind))
            {
                error = $"Disallowed action kind '{output.Action.Kind}'.";
                return false;
            }

            dto = new HomeRecommendationDto
            {
                Type = output.Type,
                Title = output.Title,
                Opportunity = output.Opportunity,
                WhyBullets = output.WhyBullets.ToArray(),
                Action = new HomeRecommendationDomainActionDto
                {
                    Kind = output.Action.Kind,
                    FeedbackId = output.Action.FeedbackId,
                    LocationGuestId = output.Action.LocationGuestId,
                    OfferId = output.Action.OfferId,
                },
                LocationName = locationName,
            };
            return true;
        }

        private static HomeRecommendationDto BuildNoneDto(string locationName)
            => new()
            {
                Type = "none",
                LocationName = locationName,
            };

        private async Task<HomeRecommendationDto?> TryGetCachedRecommendationAsync(
            string cacheKey,
            CancellationToken cancellationToken
        )
        {
            var json = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<HomeRecommendationDto>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Home recommendation cache deserialization failed for key {CacheKey}",
                    cacheKey
                );
                return null;
            }
        }

        private async Task CacheRecommendationAsync(
            string cacheKey,
            HomeRecommendationDto dto,
            CancellationToken cancellationToken
        )
        {
            var json = JsonSerializer.Serialize(dto);
            await _cache.SetStringAsync(
                cacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        HomeRecommendationContract.CacheTtl,
                },
                cancellationToken
            );
        }

        private static DateTime EnsureUtc(DateTime value)
            => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
    }
}

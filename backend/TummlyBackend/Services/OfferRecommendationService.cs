using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offer recommendation orchestrator — rules pick type, Azure writes copy,
    /// cache is operator + location + offer + Default reporting period.
    /// Free call — no credit debit.
    /// </summary>
    public sealed class OfferRecommendationService : IOfferRecommendationService
    {
        private static readonly string FailMessage =
            "Could not load a recommendation. Please try again.";

        private readonly ApplicationDbContext _context;
        private readonly IOfferRecommendationProvider _provider;
        private readonly IDistributedCache _cache;
        private readonly ILogger<OfferRecommendationService> _logger;

        public OfferRecommendationService(
            ApplicationDbContext context,
            IOfferRecommendationProvider provider,
            IDistributedCache cache,
            ILogger<OfferRecommendationService> logger
        )
        {
            _context = context;
            _provider = provider;
            _cache = cache;
            _logger = logger;
        }

        public async Task<OfferRecommendationServiceResult> RecommendAsync(
            int operatorUserId,
            int offerId,
            OfferRecommendationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.LocationId <= 0)
            {
                throw new ArgumentException("locationId is required.");
            }

            if (offerId <= 0)
            {
                return new OfferRecommendationServiceResult.NotFound();
            }

            var offerRow = await _context.CatalogOffers
                .AsNoTracking()
                .Where(offer => offer.Id == offerId)
                .Select(offer => new
                {
                    offer.Id,
                    offer.RestaurantLocationId,
                    offer.Status,
                    offer.Validity,
                    offer.CustomExpiryDate,
                    offer.Title,
                    LocationName = offer.RestaurantLocation != null
                        ? offer.RestaurantLocation.LocationName
                        : string.Empty,
                    Period = offer.RestaurantLocation != null
                        && offer.RestaurantLocation.Restaurant != null
                            ? offer.RestaurantLocation.Restaurant.DefaultReportingPeriod
                            : null,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (offerRow is null)
            {
                return new OfferRecommendationServiceResult.NotFound();
            }

            if (offerRow.RestaurantLocationId != request.LocationId)
            {
                return new OfferRecommendationServiceResult.WrongLocation();
            }

            var period = WorkspaceDefaultsOptions.NormalizeReportingPeriod(
                offerRow.Period
            );
            var (fromUtc, toUtc) = DefaultReportingPeriodWindow.Resolve(
                period,
                DateTime.UtcNow
            );

            var cacheKey = OfferRecommendationContract.BuildCacheKey(
                operatorUserId,
                request.LocationId,
                offerId,
                period
            );

            if (!request.Refresh)
            {
                var cached = await TryGetCachedRecommendationAsync(
                    cacheKey,
                    cancellationToken
                );
                if (cached is not null)
                {
                    return new OfferRecommendationServiceResult.Ok(cached);
                }
            }

            var locationName = offerRow.LocationName ?? string.Empty;
            var utcNow = DateTime.UtcNow;
            var venueToday = CatalogOfferStatus.VenueLocalToday(utcNow, 0);
            var effectiveStatus = CatalogOfferStatus.ResolveEffectiveStatus(
                offerRow.Status,
                offerRow.Validity,
                offerRow.CustomExpiryDate,
                venueToday
            );

            var facts = await LoadFactsAsync(
                offerId,
                request.LocationId,
                fromUtc,
                toUtc,
                cancellationToken
            );

            var needsAttention = CatalogOfferStatus.IsNeedsAttention(
                offerRow.Validity,
                offerRow.CustomExpiryDate,
                effectiveStatus,
                venueToday,
                hasOpenVoidRequest: facts.HasOpenVoid,
                liveAttachCount: facts.LiveAttachCount
            );

            var selectedType = SelectType(
                effectiveStatus,
                needsAttention,
                facts.MarketingEligible,
                facts.ClaimsInPeriod
            );

            if (OfferRecommendationContract.IsNone(selectedType))
            {
                var noneDto = BuildNoneDto(locationName);
                await CacheRecommendationAsync(cacheKey, noneDto, cancellationToken);
                return new OfferRecommendationServiceResult.Ok(noneDto);
            }

            OfferRecommendationProviderResult providerResult;
            try
            {
                providerResult = await _provider.RecommendAsync(
                    new OfferRecommendationProviderInput(
                        SelectedType: selectedType,
                        LocationName: locationName,
                        ReportingPeriod: period,
                        FromUtc: fromUtc,
                        ToUtc: toUtc,
                        OfferId: offerId,
                        OfferTitle: offerRow.Title,
                        MarketingEligible: facts.MarketingEligible,
                        ClaimsInPeriod: facts.ClaimsInPeriod,
                        NeedsAttention: needsAttention
                    ),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Offer recommendation provider threw for offer {OfferId}",
                    offerId
                );
                return new OfferRecommendationServiceResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            if (providerResult is OfferRecommendationProviderResult.Failed failed)
            {
                return new OfferRecommendationServiceResult.Failed(
                    FailMessage,
                    failed.Retryable
                );
            }

            if (
                providerResult
                is not OfferRecommendationProviderResult.Succeeded succeeded
            )
            {
                return new OfferRecommendationServiceResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            if (!TryMapSucceeded(
                    succeeded.Output,
                    selectedType,
                    offerId,
                    locationName,
                    out var dto,
                    out var mapError
                ))
            {
                _logger.LogWarning(
                    "Offer recommendation output rejected: {Reason}",
                    mapError
                );
                return new OfferRecommendationServiceResult.Failed(
                    FailMessage,
                    Retryable: true
                );
            }

            await CacheRecommendationAsync(cacheKey, dto, cancellationToken);
            return new OfferRecommendationServiceResult.Ok(dto);
        }

        private static string SelectType(
            string effectiveStatus,
            bool needsAttention,
            int marketingEligible,
            int claimsInPeriod
        )
        {
            if (!string.Equals(
                    effectiveStatus,
                    CatalogOfferStatus.Active,
                    StringComparison.Ordinal
                ))
            {
                return OfferRecommendationContract.TypeNone;
            }

            if (needsAttention)
            {
                return OfferRecommendationContract.TypeFix;
            }

            if (marketingEligible > 0 && claimsInPeriod == 0)
            {
                return OfferRecommendationContract.TypePromote;
            }

            return OfferRecommendationContract.TypeNone;
        }

        private async Task<(
            int MarketingEligible,
            int ClaimsInPeriod,
            bool HasOpenVoid,
            int LiveAttachCount
        )> LoadFactsAsync(
            int offerId,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests.AsNoTracking(),
                [locationId]
            );

            var marketingEligible = await GuestsListQueryComposer
                .WhereMarketingEligible(scoped)
                .CountAsync(cancellationToken);

            var claimsInPeriod = await _context.OfferIssues
                .AsNoTracking()
                .CountAsync(
                    issue =>
                        issue.CatalogOfferId == offerId
                        && issue.ClaimedAtUtc != null
                        && issue.ClaimedAtUtc >= fromUtc
                        && issue.ClaimedAtUtc < toUtc,
                    cancellationToken
                );

            var hasOpenVoid = await _context.OfferVoidRequests
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.CatalogOfferId == offerId
                        && row.Status == OfferVoidRequestStatuses.Pending,
                    cancellationToken
                );

            var liveAttachCount = await CountLiveAttachesAsync(
                offerId,
                cancellationToken
            );

            return (
                marketingEligible,
                claimsInPeriod,
                hasOpenVoid,
                liveAttachCount
            );
        }

        private async Task<int> CountLiveAttachesAsync(
            int offerId,
            CancellationToken cancellationToken
        )
        {
            var campaignCount = await _context.Campaigns
                .AsNoTracking()
                .CountAsync(
                    campaign => campaign.OfferId == offerId,
                    cancellationToken
                );
            var recoveryCount = await _context.Feedbacks
                .AsNoTracking()
                .CountAsync(
                    feedback => feedback.RecoveryOfferId == offerId,
                    cancellationToken
                );
            var thankYouCount = await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(
                    location => location.ThankYouCatalogOfferId == offerId,
                    cancellationToken
                );
            return campaignCount + recoveryCount + thankYouCount;
        }

        private static bool TryMapSucceeded(
            OfferRecommendationModelOutput output,
            string selectedType,
            int offerId,
            string locationName,
            out OfferRecommendationDto dto,
            out string? error
        )
        {
            dto = BuildNoneDto(locationName);
            error = null;

            if (!OfferRecommendationContract.IsAllowedType(output.Type))
            {
                error = $"Disallowed type '{output.Type}'.";
                return false;
            }

            if (OfferRecommendationContract.IsNone(output.Type))
            {
                error =
                    $"Provider returned none after selected type '{selectedType}'.";
                return false;
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
            )
            {
                error = "Non-none recommendation is missing required fields.";
                return false;
            }

            OfferRecommendationDraftPrefillDto? draftPrefill = null;
            if (string.Equals(
                    selectedType,
                    OfferRecommendationContract.TypePromote,
                    StringComparison.Ordinal
                ))
            {
                var channel = output.SuggestedChannel is "sms" or "email"
                    ? output.SuggestedChannel
                    : "email";
                draftPrefill = new OfferRecommendationDraftPrefillDto
                {
                    OfferId = offerId,
                    OfferStance = "existing-offer",
                    GoalId = "promote-something-new",
                    AudienceKey = "all-eligible-guests",
                    Channel = channel,
                    CampaignName = output.CampaignName ?? string.Empty,
                    MessageSubject = output.MessageSubject,
                    MessageBody = output.MessageBody,
                };
            }

            dto = new OfferRecommendationDto
            {
                Type = output.Type,
                Title = output.Title,
                Opportunity = output.Opportunity,
                WhyBullets = output.WhyBullets.ToArray(),
                SuggestedChannel = output.SuggestedChannel,
                DraftPrefill = draftPrefill,
                LocationName = locationName,
            };
            return true;
        }

        private static OfferRecommendationDto BuildNoneDto(string locationName)
            => new()
            {
                Type = OfferRecommendationContract.TypeNone,
                LocationName = locationName,
            };

        private async Task<OfferRecommendationDto?> TryGetCachedRecommendationAsync(
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
                return JsonSerializer.Deserialize<OfferRecommendationDto>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Offer recommendation cache deserialization failed for key {CacheKey}",
                    cacheKey
                );
                return null;
            }
        }

        private async Task CacheRecommendationAsync(
            string cacheKey,
            OfferRecommendationDto dto,
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
                        OfferRecommendationContract.CacheTtl,
                },
                cancellationToken
            );
        }
    }
}

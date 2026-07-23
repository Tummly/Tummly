using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestFeedbacksListService : IGuestFeedbacksListService
    {
        public const int DefaultPageSize = 25;

        private readonly ApplicationDbContext _context;

        public GuestFeedbacksListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GuestFeedbacksListResponse?> ListAsync(
            int locationGuestId,
            int locationId,
            string locationName,
            string? q,
            string[]? sentiment,
            string[]? detectedTags,
            string? datePreset,
            DateTime? dateFrom,
            DateTime? dateTo,
            string sort,
            int page,
            int pageSize,
            int utcOffsetMinutes,
            CancellationToken cancellationToken = default
        )
        {
            var guestExists =
                await GuestScopedListValidation.EnsureLocationGuestExistsAsync(
                    _context,
                    locationGuestId,
                    locationId,
                    cancellationToken
                );

            if (!guestExists)
            {
                return null;
            }

            var sortKey = GuestScopedListValidation.ValidatePagingAndSort(
                page,
                pageSize,
                sort,
                DefaultPageSize
            );
            var (rangeFrom, rangeTo) =
                GuestScopedListValidation.ResolveOptionalDateWindow(
                    datePreset,
                    dateFrom,
                    dateTo,
                    utcOffsetMinutes
                );

            var sentiments = NormalizeSentiments(sentiment);
            var tagKeys = NormalizeDetectedTags(detectedTags);

            var query = _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.LocationGuestId == locationGuestId
                    && f.RestaurantLocationId == locationId
                );

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                query = query.Where(f => f.Comment.Contains(needle));
            }

            if (sentiments.Count > 0)
            {
                query = query.Where(f =>
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment != null
                    && sentiments.Contains(f.Sentiment.Value)
                );
            }

            if (tagKeys.Count > 0)
            {
                var includeFoodQuality = tagKeys.Contains(nameof(DetectedTag.FoodQuality));
                var includeService = tagKeys.Contains(nameof(DetectedTag.Service));
                var includeWaitTime = tagKeys.Contains(nameof(DetectedTag.WaitTime));
                var includeCleanliness = tagKeys.Contains(nameof(DetectedTag.Cleanliness));
                var includeValue = tagKeys.Contains(nameof(DetectedTag.Value));
                var includeAtmosphere = tagKeys.Contains(nameof(DetectedTag.Atmosphere));
                var includeBilling = tagKeys.Contains(nameof(DetectedTag.Billing));
                var includeAllergiesDietary = tagKeys.Contains(
                    nameof(DetectedTag.AllergiesDietary)
                );
                var includeBookingSeating = tagKeys.Contains(
                    nameof(DetectedTag.BookingSeating)
                );
                var includeOther = tagKeys.Contains(nameof(DetectedTag.Other));

                query = query.Where(f =>
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.DetectedTagsJson != null
                    && (
                        (includeFoodQuality
                            && f.DetectedTagsJson.Contains("\"FoodQuality\""))
                        || (includeService
                            && f.DetectedTagsJson.Contains("\"Service\""))
                        || (includeWaitTime
                            && f.DetectedTagsJson.Contains("\"WaitTime\""))
                        || (includeCleanliness
                            && f.DetectedTagsJson.Contains("\"Cleanliness\""))
                        || (includeValue
                            && f.DetectedTagsJson.Contains("\"Value\""))
                        || (includeAtmosphere
                            && f.DetectedTagsJson.Contains("\"Atmosphere\""))
                        || (includeBilling
                            && f.DetectedTagsJson.Contains("\"Billing\""))
                        || (includeAllergiesDietary
                            && f.DetectedTagsJson.Contains("\"AllergiesDietary\""))
                        || (includeBookingSeating
                            && f.DetectedTagsJson.Contains("\"BookingSeating\""))
                        || (includeOther
                            && f.DetectedTagsJson.Contains("\"Other\""))
                    )
                );
            }

            if (rangeFrom.HasValue && rangeTo.HasValue)
            {
                var from = rangeFrom.Value;
                var to = rangeTo.Value;
                query = query.Where(f =>
                    f.CreatedAt >= from && f.CreatedAt < to
                );
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = sortKey == "oldest-first"
                ? query.OrderBy(f => f.CreatedAt).ThenBy(f => f.Id)
                : query
                    .OrderByDescending(f => f.CreatedAt)
                    .ThenByDescending(f => f.Id);

            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(f =>
                {
                    var classification =
                        FeedbackClassificationMapping.ToApiFields(f);

                    return new GuestFeedbacksListItemDto
                    {
                        Id = f.Id,
                        CreatedAt = f.CreatedAt,
                        Comment = f.Comment,
                        LocationName = locationName,
                        ClassificationStatus =
                            classification.ClassificationStatus,
                        Sentiment = classification.Sentiment,
                        DetectedTags = classification.DetectedTags,
                    };
                })
                .ToList();

            return new GuestFeedbacksListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        private static List<FeedbackSentiment> NormalizeSentiments(
            string[]? sentiment
        )
        {
            if (sentiment is not { Length: > 0 })
            {
                return [];
            }

            GuestsFilterOptions.Validate(
                marketing: [],
                contact: [],
                sentiment: sentiment
            );

            var normalized = GuestsFilterOptions.Normalize(
                sentiment,
                GuestsFilterOptions.Sentiment
            );

            return normalized
                .Select(wire =>
                {
                    if (!FeedbackClassificationMapping.TryParseWireSentiment(
                            wire,
                            out var value
                        ))
                    {
                        throw new ArgumentException("Invalid sentiment value.");
                    }

                    return value;
                })
                .Distinct()
                .ToList();
        }

        private static HashSet<string> NormalizeDetectedTags(
            string[]? detectedTags
        )
        {
            if (detectedTags is not { Length: > 0 })
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var allowed = Enum.GetNames<DetectedTag>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new HashSet<string>(StringComparer.Ordinal);

            foreach (var raw in detectedTags)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                if (!allowed.Contains(raw))
                {
                    throw new ArgumentException(
                        $"Invalid detectedTags value '{raw}'."
                    );
                }

                var canonical = Enum.GetNames<DetectedTag>()
                    .Single(name =>
                        name.Equals(raw, StringComparison.OrdinalIgnoreCase)
                    );
                result.Add(canonical);
            }

            return result;
        }
    }
}

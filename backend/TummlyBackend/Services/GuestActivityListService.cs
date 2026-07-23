using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class GuestActivityListService : IGuestActivityListService
    {
        public const int DefaultPageSize = 25;

        private readonly ApplicationDbContext _context;

        public GuestActivityListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GuestActivityListResponse?> ListAsync(
            int locationGuestId,
            int locationId,
            string locationName,
            string[]? types,
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
            var guestExists = await _context.LocationGuests
                .AsNoTracking()
                .AnyAsync(
                    lg =>
                        lg.Id == locationGuestId
                        && lg.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (!guestExists)
            {
                return null;
            }

            if (page < 1)
            {
                throw new ArgumentException("page must be >= 1.");
            }

            if (pageSize != DefaultPageSize)
            {
                throw new ArgumentException(
                    $"pageSize must be {DefaultPageSize}."
                );
            }

            var sortKey = (sort ?? string.Empty).Trim().ToLowerInvariant();
            if (
                sortKey is not ("recent-activity" or "oldest-first")
            )
            {
                throw new ArgumentException(
                    "sort must be recent-activity or oldest-first."
                );
            }

            DateTime? rangeFrom = null;
            DateTime? rangeTo = null;

            var hasPreset = !string.IsNullOrWhiteSpace(datePreset);
            var hasCustom = dateFrom.HasValue || dateTo.HasValue;

            if (hasPreset && hasCustom)
            {
                throw new ArgumentException(
                    "datePreset and dateFrom/dateTo are mutually exclusive."
                );
            }

            if (hasPreset)
            {
                if (!GuestsDateWindows.IsValidTablePreset(datePreset!))
                {
                    throw new ArgumentException("Invalid datePreset.");
                }

                (rangeFrom, rangeTo) = GuestsDateWindows.ResolvePreset(
                    datePreset!,
                    DateTime.UtcNow,
                    utcOffsetMinutes
                );
            }
            else if (hasCustom)
            {
                if (!dateFrom.HasValue || !dateTo.HasValue)
                {
                    throw new ArgumentException(
                        "dateFrom and dateTo are both required for a custom range."
                    );
                }

                (rangeFrom, rangeTo) = GuestsDateWindows.ResolveCustom(
                    dateFrom.Value,
                    dateTo.Value
                );
            }

            HashSet<string>? kindFilter = null;
            if (types is { Length: > 0 })
            {
                kindFilter = new HashSet<string>(StringComparer.Ordinal);
                foreach (var type in types)
                {
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    var kinds = LocationGuestActivityKinds.KindsForFilterType(
                        type
                    );
                    if (kinds == null)
                    {
                        throw new ArgumentException(
                            $"Unknown activity type '{type}'."
                        );
                    }

                    foreach (var kind in kinds)
                    {
                        kindFilter.Add(kind);
                    }
                }

                if (kindFilter.Count == 0)
                {
                    kindFilter = null;
                }
            }

            var query = _context.LocationGuestActivityEvents
                .AsNoTracking()
                .Where(e => e.LocationGuestId == locationGuestId);

            if (kindFilter != null)
            {
                query = query.Where(e => kindFilter.Contains(e.Kind));
            }

            if (rangeFrom.HasValue && rangeTo.HasValue)
            {
                var from = rangeFrom.Value;
                var to = rangeTo.Value;
                query = query.Where(e =>
                    e.OccurredAt >= from && e.OccurredAt < to
                );
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = sortKey == "oldest-first"
                ? query
                    .OrderBy(e => e.OccurredAt)
                    .ThenBy(e => e.Id)
                : query
                    .OrderByDescending(e => e.OccurredAt)
                    .ThenByDescending(e => e.Id);

            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // `feedback` events are append-only at submit (usually Pending). PRD
            // row copy needs current classification honesty from Feedback itself.
            var feedbackIdsForSentiment = rows
                .Where(row =>
                    row.Kind == LocationGuestActivityKinds.Feedback
                    && row.FeedbackId != null
                )
                .Select(row => row.FeedbackId!.Value)
                .Distinct()
                .ToList();

            Dictionary<int, string> feedbackSentiments = new();
            if (feedbackIdsForSentiment.Count > 0)
            {
                var classified = await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        feedbackIdsForSentiment.Contains(f.Id)
                        && f.ClassificationStatus
                            == ClassificationStatus.Succeeded
                        && f.Sentiment != null
                    )
                    .Select(f => new { f.Id, f.Sentiment })
                    .ToListAsync(cancellationToken);

                foreach (var row in classified)
                {
                    var wire = FeedbackClassificationMapping.ToWireSentiment(
                        row.Sentiment
                    );
                    if (wire != null)
                    {
                        feedbackSentiments[row.Id] = wire;
                    }
                }
            }

            var items = rows
                .Select(row =>
                {
                    var payload = LocationGuestActivityPayload.Deserialize(
                        row.PayloadJson
                    );

                    string? sentiment = payload?.Sentiment;
                    if (
                        row.Kind == LocationGuestActivityKinds.Feedback
                        && row.FeedbackId is { } feedbackId
                        && feedbackSentiments.TryGetValue(
                            feedbackId,
                            out var liveSentiment
                        )
                    )
                    {
                        sentiment = liveSentiment;
                    }

                    return new GuestActivityListItemDto
                    {
                        Id = row.Id,
                        Kind = row.Kind,
                        OccurredAt = row.OccurredAt,
                        FeedbackId = row.FeedbackId,
                        LocationName = locationName,
                        TagName = payload?.TagName,
                        GuestTagId = payload?.GuestTagId,
                        AuthorDisplayName = payload?.AuthorDisplayName,
                        Sentiment = sentiment,
                        ChangedFields = payload?.ChangedFields,
                    };
                })
                .ToList();

            return new GuestActivityListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }
    }
}

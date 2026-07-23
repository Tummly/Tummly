using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class LocationGuestActivityBackfillService
        : ILocationGuestActivityBackfillService
    {
        private const int SaveBatchSize = 200;

        private readonly ApplicationDbContext _context;

        public LocationGuestActivityBackfillService(
            ApplicationDbContext context
        )
        {
            _context = context;
        }

        public async Task BackfillAsync(
            CancellationToken cancellationToken = default
        )
        {
            await BackfillGuestJoinedAsync(cancellationToken);
            await BackfillFeedbackAsync(cancellationToken);
            await BackfillTagAppliedAsync(cancellationToken);
            await BackfillClassificationTerminalAsync(cancellationToken);
        }

        private async Task BackfillGuestJoinedAsync(
            CancellationToken cancellationToken
        )
        {
            // Cheap gate: any Location Guest missing a GuestJoined event?
            var needsWork = await _context.LocationGuests
                .AsNoTracking()
                .AnyAsync(
                    lg => !_context.LocationGuestActivityEvents.Any(e =>
                        e.Kind == LocationGuestActivityKinds.GuestJoined
                        && e.LocationGuestId == lg.Id
                    ),
                    cancellationToken
                );

            if (!needsWork)
            {
                return;
            }

            var existingGuestIds = await _context.LocationGuestActivityEvents
                .AsNoTracking()
                .Where(e =>
                    e.Kind == LocationGuestActivityKinds.GuestJoined
                    && e.LocationGuestId != null
                )
                .Select(e => e.LocationGuestId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var existing = existingGuestIds.ToHashSet();

            var guests = await _context.LocationGuests
                .AsNoTracking()
                .Select(lg => new { lg.Id, lg.CreatedAt })
                .ToListAsync(cancellationToken);

            var pending = 0;
            var now = DateTime.UtcNow;

            foreach (var guest in guests)
            {
                if (existing.Contains(guest.Id))
                {
                    continue;
                }

                _context.LocationGuestActivityEvents.Add(
                    new LocationGuestActivityEvent
                    {
                        LocationGuestId = guest.Id,
                        Kind = LocationGuestActivityKinds.GuestJoined,
                        OccurredAt = guest.CreatedAt,
                        CreatedAt = now,
                    }
                );
                pending++;

                if (pending >= SaveBatchSize)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    pending = 0;
                }
            }

            if (pending > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task BackfillFeedbackAsync(
            CancellationToken cancellationToken
        )
        {
            // Cheap gate: any Feedback missing a Feedback activity event?
            var needsWork = await _context.Feedbacks
                .AsNoTracking()
                .AnyAsync(
                    f => !_context.LocationGuestActivityEvents.Any(e =>
                        e.Kind == LocationGuestActivityKinds.Feedback
                        && e.FeedbackId == f.Id
                    ),
                    cancellationToken
                );

            if (!needsWork)
            {
                return;
            }

            var existingFeedbackIds = await _context.LocationGuestActivityEvents
                .AsNoTracking()
                .Where(e =>
                    e.Kind == LocationGuestActivityKinds.Feedback
                    && e.FeedbackId != null
                )
                .Select(e => e.FeedbackId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var existing = existingFeedbackIds.ToHashSet();

            var feedbacks = await _context.Feedbacks
                .AsNoTracking()
                .Select(f => new
                {
                    f.Id,
                    f.LocationGuestId,
                    f.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var pending = 0;
            var now = DateTime.UtcNow;

            foreach (var feedback in feedbacks)
            {
                if (existing.Contains(feedback.Id))
                {
                    continue;
                }

                _context.LocationGuestActivityEvents.Add(
                    new LocationGuestActivityEvent
                    {
                        LocationGuestId = feedback.LocationGuestId,
                        FeedbackId = feedback.Id,
                        Kind = LocationGuestActivityKinds.Feedback,
                        OccurredAt = feedback.CreatedAt,
                        CreatedAt = now,
                    }
                );
                pending++;

                if (pending >= SaveBatchSize)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    pending = 0;
                }
            }

            if (pending > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task BackfillTagAppliedAsync(
            CancellationToken cancellationToken
        )
        {
            // Cheap gate: membership count vs TagApplied event count.
            // Payload-matched EXISTS is expensive; for this finite migration,
            // equal counts mean the heavy path has nothing left to insert
            // (emitters + prior backfill keep ~1:1). If counts diverge, run.
            var membershipCount = await _context.LocationGuestTags
                .AsNoTracking()
                .CountAsync(cancellationToken);
            var tagAppliedCount = await _context.LocationGuestActivityEvents
                .AsNoTracking()
                .CountAsync(
                    e => e.Kind == LocationGuestActivityKinds.TagApplied,
                    cancellationToken
                );

            if (membershipCount == tagAppliedCount)
            {
                return;
            }

            var existingPairs = await _context.LocationGuestActivityEvents
                .AsNoTracking()
                .Where(e =>
                    e.Kind == LocationGuestActivityKinds.TagApplied
                    && e.LocationGuestId != null
                    && e.PayloadJson != null
                )
                .Select(e => new { e.LocationGuestId, e.PayloadJson })
                .ToListAsync(cancellationToken);

            var existing = new HashSet<(int GuestId, int TagId)>();
            foreach (var row in existingPairs)
            {
                var payload = LocationGuestActivityPayload.Deserialize(
                    row.PayloadJson
                );
                if (
                    row.LocationGuestId is int guestId
                    && payload?.GuestTagId is int tagId
                )
                {
                    existing.Add((guestId, tagId));
                }
            }

            var memberships = await _context.LocationGuestTags
                .AsNoTracking()
                .Select(m => new
                {
                    m.LocationGuestId,
                    m.GuestTagId,
                    TagName = m.GuestTag!.DisplayName,
                    m.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var pending = 0;
            var now = DateTime.UtcNow;

            foreach (var membership in memberships)
            {
                if (
                    existing.Contains(
                        (membership.LocationGuestId, membership.GuestTagId)
                    )
                )
                {
                    continue;
                }

                _context.LocationGuestActivityEvents.Add(
                    new LocationGuestActivityEvent
                    {
                        LocationGuestId = membership.LocationGuestId,
                        Kind = LocationGuestActivityKinds.TagApplied,
                        OccurredAt = membership.CreatedAt,
                        CreatedAt = now,
                        PayloadJson = LocationGuestActivityPayload.Serialize(
                            new LocationGuestActivityPayload
                            {
                                GuestTagId = membership.GuestTagId,
                                TagName = membership.TagName,
                            }
                        ),
                    }
                );
                pending++;

                if (pending >= SaveBatchSize)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    pending = 0;
                }
            }

            if (pending > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task BackfillClassificationTerminalAsync(
            CancellationToken cancellationToken
        )
        {
            // Cheap gate: any Succeeded/Failed Feedback missing a terminal
            // classification activity event?
            var needsWork = await _context.Feedbacks
                .AsNoTracking()
                .AnyAsync(
                    f =>
                        (
                            f.ClassificationStatus
                                == ClassificationStatus.Succeeded
                            || f.ClassificationStatus
                                == ClassificationStatus.Failed
                        )
                        && !_context.LocationGuestActivityEvents.Any(e =>
                            e.FeedbackId == f.Id
                            && (
                                e.Kind
                                    == LocationGuestActivityKinds
                                        .ClassificationSucceeded
                                || e.Kind
                                    == LocationGuestActivityKinds
                                        .ClassificationFailed
                            )
                        ),
                    cancellationToken
                );

            if (!needsWork)
            {
                return;
            }

            // One terminal event per Feedback for current Succeeded/Failed.
            // Reopen history is not reconstructable from Feedback alone.
            var existingFeedbackIds = await _context.LocationGuestActivityEvents
                .AsNoTracking()
                .Where(e =>
                    (
                        e.Kind
                            == LocationGuestActivityKinds
                                .ClassificationSucceeded
                        || e.Kind
                            == LocationGuestActivityKinds.ClassificationFailed
                    )
                    && e.FeedbackId != null
                )
                .Select(e => e.FeedbackId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var existing = existingFeedbackIds.ToHashSet();

            var feedbacks = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    || f.ClassificationStatus == ClassificationStatus.Failed
                )
                .Select(f => new
                {
                    f.Id,
                    f.LocationGuestId,
                    f.ClassificationStatus,
                    f.Sentiment,
                    f.ClassificationClaimedAt,
                    f.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            var pending = 0;
            var now = DateTime.UtcNow;

            foreach (var feedback in feedbacks)
            {
                if (existing.Contains(feedback.Id))
                {
                    continue;
                }

                var kind =
                    feedback.ClassificationStatus
                        == ClassificationStatus.Succeeded
                        ? LocationGuestActivityKinds.ClassificationSucceeded
                        : LocationGuestActivityKinds.ClassificationFailed;

                // Documented approximation: claim stamp if present, else CreatedAt.
                var occurredAt =
                    feedback.ClassificationClaimedAt ?? feedback.CreatedAt;

                string? payloadJson = null;
                if (
                    feedback.ClassificationStatus
                        == ClassificationStatus.Succeeded
                    && feedback.Sentiment is { } sentiment
                )
                {
                    payloadJson = LocationGuestActivityPayload.Serialize(
                        new LocationGuestActivityPayload
                        {
                            Sentiment =
                                FeedbackClassificationMapping.ToWireSentiment(
                                    sentiment
                                ),
                        }
                    );
                }

                _context.LocationGuestActivityEvents.Add(
                    new LocationGuestActivityEvent
                    {
                        LocationGuestId = feedback.LocationGuestId,
                        FeedbackId = feedback.Id,
                        Kind = kind,
                        OccurredAt = occurredAt,
                        CreatedAt = now,
                        PayloadJson = payloadJson,
                    }
                );
                pending++;

                if (pending >= SaveBatchSize)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    pending = 0;
                }
            }

            if (pending > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

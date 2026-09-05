using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Feedback report aggregate — Area Reports KPIs, status, needs-attention, by-source.
    /// </summary>
    public sealed class ReportsFeedbackService : IReportsFeedbackService
    {
        private const int NeedsAttentionCap = 5;
        private const int CommentPreviewMaxLength = 80;

        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _capture;

        public ReportsFeedbackService(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate capture
        )
        {
            _context = context;
            _capture = capture;
        }

        public async Task<ReportsFeedbackDto> GetFeedbackReportAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            if (await IsLifetimeEmptyAsync(locationId, cancellationToken))
            {
                return new ReportsFeedbackDto { LifetimeEmpty = true };
            }

            var span = toUtc - fromUtc;
            var previousFromUtc = fromUtc - span;
            var previousToUtc = fromUtc;

            var locationIds = new[] { locationId };
            var activeOrPausedQrIds =
                await _capture.ListActiveOrPausedQrCodeIdsAsync(locationIds);

            var feedbackReceived = await MetricPairAsync(
                () =>
                    CountFeedbackAtLocationAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        marketingOptInOnly: false,
                        cancellationToken
                    ),
                () =>
                    CountFeedbackAtLocationAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        marketingOptInOnly: false,
                        cancellationToken
                    )
            );

            // Same grain as Capture / hub: Active + Paused QR, !OffersOptOut.
            var marketingOptIns = await MetricPairAsync(
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        fromUtc,
                        toUtc,
                        marketingOptInOnly: true
                    ),
                () =>
                    _capture.CountFeedbackAsync(
                        locationIds,
                        activeOrPausedQrIds,
                        previousFromUtc,
                        previousToUtc,
                        marketingOptInOnly: true
                    )
            );

            var followUpNeeded = await MetricPairAsync(
                () =>
                    CountNeedsAttentionAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    ),
                () =>
                    CountNeedsAttentionAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        cancellationToken
                    )
            );

            var resolved = await MetricPairAsync(
                () =>
                    CountWorkflowStatusAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        FeedbackWorkflowStatus.Resolved,
                        cancellationToken
                    ),
                () =>
                    CountWorkflowStatusAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        FeedbackWorkflowStatus.Resolved,
                        cancellationToken
                    )
            );

            var statusNew = await MetricPairAsync(
                () =>
                    CountWorkflowStatusAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        FeedbackWorkflowStatus.New,
                        cancellationToken
                    ),
                () =>
                    CountWorkflowStatusAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        FeedbackWorkflowStatus.New,
                        cancellationToken
                    )
            );

            var statusInProgress = await MetricPairAsync(
                () =>
                    CountWorkflowStatusAsync(
                        locationId,
                        fromUtc,
                        toUtc,
                        FeedbackWorkflowStatus.InProgress,
                        cancellationToken
                    ),
                () =>
                    CountWorkflowStatusAsync(
                        locationId,
                        previousFromUtc,
                        previousToUtc,
                        FeedbackWorkflowStatus.InProgress,
                        cancellationToken
                    )
            );

            var needsAttention = await BuildNeedsAttentionListAsync(
                locationId,
                fromUtc,
                toUtc,
                cancellationToken
            );

            var bySource = await BuildBySourceAsync(
                locationId,
                activeOrPausedQrIds,
                fromUtc,
                toUtc,
                cancellationToken
            );

            return new ReportsFeedbackDto
            {
                LifetimeEmpty = false,
                Kpis = new ReportsFeedbackKpisDto
                {
                    FeedbackReceived = feedbackReceived,
                    MarketingOptIns = marketingOptIns,
                    FollowUpNeeded = followUpNeeded,
                    Resolved = resolved,
                },
                Status = new ReportsFeedbackStatusStripDto
                {
                    New = statusNew,
                    InProgress = statusInProgress,
                    FollowUpNeeded = followUpNeeded,
                    Resolved = resolved,
                },
                NeedsAttention = needsAttention,
                BySource = bySource,
            };
        }

        private async Task<bool> IsLifetimeEmptyAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var hasFeedback = await _context.Feedbacks
                .AsNoTracking()
                .AnyAsync(
                    f => f.RestaurantLocationId == locationId,
                    cancellationToken
                );
            return !hasFeedback;
        }

        private async Task<
            IReadOnlyList<ReportsFeedbackNeedsAttentionDto>
        > BuildNeedsAttentionListAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            var rows = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                    && f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment == FeedbackSentiment.Negative
                    && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(NeedsAttentionCap)
                .Select(f => new
                {
                    f.Id,
                    f.CreatedAt,
                    f.GuestName,
                    f.Comment,
                    f.WorkflowStatus,
                    f.QrCodeId,
                    QrType = f.QrCode!.QrType,
                    LinkName = f.QrCode!.LinkName,
                })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => new ReportsFeedbackNeedsAttentionDto
                {
                    FeedbackId = row.Id,
                    SubmittedAt = row.CreatedAt,
                    GuestName = row.GuestName,
                    Source = SourceLabel(row.LinkName, row.QrType),
                    CommentPreview = PreviewComment(row.Comment),
                    WorkflowStatus = WorkflowStatusLabel(row.WorkflowStatus),
                })
                .ToList();
        }

        private async Task<
            IReadOnlyList<ReportsFeedbackBySourceDto>
        > BuildBySourceAsync(
            int locationId,
            IReadOnlyList<int> activeOrPausedQrIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            if (activeOrPausedQrIds.Count == 0)
            {
                return [];
            }

            var qrRows = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && activeOrPausedQrIds.Contains(q.Id)
                )
                .Select(q => new
                {
                    q.Id,
                    q.QrType,
                    q.LinkName,
                })
                .ToListAsync(cancellationToken);

            var feedbackByQr = (
                await _capture.GroupFeedbackByQrCodeAsync(
                    activeOrPausedQrIds,
                    fromUtc,
                    toUtc
                )
            ).ToDictionary(
                r => r.QrCodeId,
                r => (r.FeedbackSubmitted, r.MarketingOptIns)
            );

            var followUpByQr = await _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && activeOrPausedQrIds.Contains(f.QrCodeId)
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                    && f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment == FeedbackSentiment.Negative
                    && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
                )
                .GroupBy(f => f.QrCodeId)
                .Select(g => new { QrCodeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(
                    r => r.QrCodeId,
                    r => r.Count,
                    cancellationToken
                );

            return qrRows
                .Select(q =>
                {
                    feedbackByQr.TryGetValue(q.Id, out var feedback);
                    followUpByQr.TryGetValue(q.Id, out var followUp);
                    return new ReportsFeedbackBySourceDto
                    {
                        QrCodeId = q.Id,
                        Source = SourceLabel(q.LinkName, q.QrType),
                        Feedback = feedback.FeedbackSubmitted,
                        MarketingOptIns = feedback.MarketingOptIns,
                        FollowUpNeeded = followUp,
                    };
                })
                .Where(row => row.Feedback > 0 || row.MarketingOptIns > 0)
                .OrderByDescending(row => row.Feedback)
                .ToList();
        }

        private Task<int> CountFeedbackAtLocationAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            bool marketingOptInOnly,
            CancellationToken cancellationToken
        )
        {
            var query = _context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                );

            if (marketingOptInOnly)
            {
                query = query.Where(f => !f.OffersOptOut);
            }

            return query.CountAsync(cancellationToken);
        }

        private Task<int> CountNeedsAttentionAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken
        )
        {
            return _context.Feedbacks
                .AsNoTracking()
                .CountAsync(
                    f =>
                        f.RestaurantLocationId == locationId
                        && f.CreatedAt >= fromUtc
                        && f.CreatedAt < toUtc
                        && f.ClassificationStatus
                            == ClassificationStatus.Succeeded
                        && f.Sentiment == FeedbackSentiment.Negative
                        && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved,
                    cancellationToken
                );
        }

        private Task<int> CountWorkflowStatusAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            FeedbackWorkflowStatus status,
            CancellationToken cancellationToken
        )
        {
            return _context.Feedbacks
                .AsNoTracking()
                .CountAsync(
                    f =>
                        f.RestaurantLocationId == locationId
                        && f.CreatedAt >= fromUtc
                        && f.CreatedAt < toUtc
                        && f.WorkflowStatus == status,
                    cancellationToken
                );
        }

        private static string SourceLabel(string? linkName, QrType qrType)
        {
            if (!string.IsNullOrWhiteSpace(linkName))
            {
                return linkName.Trim();
            }

            return FeedbackQrSourceMapping.ToDisplay(
                    new QrCode { QrType = qrType, LinkName = null }
                )
                ?? qrType.ToString();
        }

        private static string PreviewComment(string comment)
        {
            var trimmed = (comment ?? string.Empty).Trim();
            if (trimmed.Length <= CommentPreviewMaxLength)
            {
                return trimmed;
            }

            return trimmed[..CommentPreviewMaxLength].TrimEnd() + "…";
        }

        private static string WorkflowStatusLabel(FeedbackWorkflowStatus status)
        {
            return status switch
            {
                FeedbackWorkflowStatus.InProgress => "In progress",
                FeedbackWorkflowStatus.Resolved => "Resolved",
                _ => "New",
            };
        }

        private static async Task<ReportsMetricDto> MetricPairAsync(
            Func<Task<int>> current,
            Func<Task<int>> previous
        )
        {
            return Metric(await current(), await previous());
        }

        private static ReportsMetricDto Metric(int value, int valuePrevious)
        {
            return new ReportsMetricDto
            {
                Value = value,
                ValuePrevious = valuePrevious,
            };
        }
    }
}

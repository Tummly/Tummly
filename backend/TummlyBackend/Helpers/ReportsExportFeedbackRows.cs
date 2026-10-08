using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Reports;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Windowed Feedback row snapshot for Reports PDF / CSV (REP-02 / REP-03).
    /// </summary>
    public static class ReportsExportFeedbackRows
    {
        public const int SoftMaxRows = 50;

        public static async Task<
            IReadOnlyList<ReportsExportFeedbackRowDto>
        > LoadAsync(
            ApplicationDbContext context,
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await context.Feedbacks
                .AsNoTracking()
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= fromUtc
                    && f.CreatedAt < toUtc
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(SoftMaxRows)
                .Select(f => new
                {
                    f.Id,
                    f.CreatedAt,
                    f.Comment,
                    f.DetectedTagsJson,
                    f.WorkflowStatus,
                    f.GuestName,
                    f.LocationGuestId,
                    QrType = f.QrCode != null ? f.QrCode.QrType : (QrType?)null,
                    QrLinkName = f.QrCode != null ? f.QrCode.LinkName : null,
                })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row =>
                {
                    var source =
                        row.QrType == null
                            ? string.Empty
                            : FeedbackQrSourceMapping.ToDisplay(
                                  new QrCode
                                  {
                                      QrType = row.QrType.Value,
                                      LinkName = row.QrLinkName,
                                  }
                              )
                                ?? row.QrType.Value.ToString();
                    return new ReportsExportFeedbackRowDto
                    {
                        FeedbackId = row.Id,
                        CreatedAtUtc = row.CreatedAt,
                        Tags = FormatTags(row.DetectedTagsJson),
                        Comment = Truncate(row.Comment, 160),
                        Source = source,
                        Status = FormatStatus(row.WorkflowStatus),
                        FollowUp = FormatFollowUp(row.WorkflowStatus),
                        Guest = FormatGuest(row.LocationGuestId, row.GuestName),
                    };
                })
                .ToList();
        }

        public static string[] Headers { get; } =
        [
            "Feedback ID",
            "Date",
            "Tags",
            "Comment",
            "Source",
            "Status",
            "Follow-up",
            "Guest",
        ];

        public static string[] ToCsvRow(ReportsExportFeedbackRowDto row)
            =>
            [
                row.FeedbackId.ToString(CultureInfo.InvariantCulture),
                row.CreatedAtUtc.ToString(
                    "dd MMM yyyy",
                    CultureInfo.InvariantCulture
                ),
                row.Tags,
                row.Comment,
                row.Source,
                row.Status,
                row.FollowUp,
                row.Guest,
            ];

        private static string FormatTags(string? json)
        {
            var keys = FeedbackClassificationMapping.DeserializeDetectedTagKeys(
                json
            );
            if (keys is null || keys.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("; ", keys);
        }

        private static string FormatStatus(FeedbackWorkflowStatus status)
            => status switch
            {
                FeedbackWorkflowStatus.New => "New",
                FeedbackWorkflowStatus.InProgress => "In progress",
                FeedbackWorkflowStatus.Resolved => "Resolved",
                _ => status.ToString(),
            };

        private static string FormatFollowUp(FeedbackWorkflowStatus status)
            => status switch
            {
                FeedbackWorkflowStatus.Resolved => "Followed up",
                FeedbackWorkflowStatus.New => "Open",
                FeedbackWorkflowStatus.InProgress => "Open",
                _ => "Open",
            };

        private static string FormatGuest(int? locationGuestId, string guestName)
        {
            var name = string.IsNullOrWhiteSpace(guestName)
                ? "Guest"
                : guestName.Trim();
            if (locationGuestId is int id)
            {
                return $"G-{id} / {name}";
            }

            return name;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value ?? string.Empty;
            }

            return value[..(max - 3)] + "...";
        }
    }
}

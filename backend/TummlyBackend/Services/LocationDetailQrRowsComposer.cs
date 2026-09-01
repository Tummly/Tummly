using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// QR table rows from Capture snapshot placements for Location detail (ticket 02).
    /// </summary>
    public sealed class LocationDetailQrRowsComposer
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _engagement;

        public LocationDetailQrRowsComposer(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate engagement
        )
        {
            _context = context;
            _engagement = engagement;
        }

        public async Task<IReadOnlyList<LocationDetailQrRowDto>> ComposeAsync(
            int locationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            var qrCodes = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    q.RestaurantLocationId == locationId
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .OrderBy(q => q.QrType)
                .ThenBy(q => q.LinkName)
                .ToListAsync(cancellationToken);

            if (qrCodes.Count == 0)
            {
                return Array.Empty<LocationDetailQrRowDto>();
            }

            var qrCodeIds = qrCodes.Select(q => q.Id).ToList();
            var windowedScans = await _engagement.GroupScansByQrCodeAsync(
                qrCodeIds,
                fromUtc,
                toUtc
            );
            var windowedFeedback = await _engagement.GroupFeedbackByQrCodeAsync(
                qrCodeIds,
                fromUtc,
                toUtc
            );

            var scanLookup = windowedScans.ToDictionary(
                x => x.QrCodeId,
                x => x.Count
            );
            var feedbackLookup = windowedFeedback.ToDictionary(
                x => x.QrCodeId
            );

            var lastScans = await _context.QrScanEvents
                .AsNoTracking()
                .Where(e =>
                    e.QrCodeId != null && qrCodeIds.Contains(e.QrCodeId.Value)
                )
                .GroupBy(e => e.QrCodeId!.Value)
                .Select(g => new
                {
                    QrCodeId = g.Key,
                    LastScanAt = g.Max(e => e.CreatedAt),
                })
                .ToListAsync(cancellationToken);

            var lastScanLookup = lastScans.ToDictionary(
                x => x.QrCodeId,
                x => x.LastScanAt
            );

            return qrCodes
                .Select(qr =>
                {
                    feedbackLookup.TryGetValue(qr.Id, out var feedback);
                    lastScanLookup.TryGetValue(qr.Id, out var lastScanAt);

                    return new LocationDetailQrRowDto
                    {
                        QrCodeId = qr.Id,
                        Name = ResolveName(qr),
                        Placement = ResolvePlacement(qr),
                        StatusLabel = FormatStatusLabel(qr.Status),
                        Scans = scanLookup.GetValueOrDefault(qr.Id),
                        Starts = 0,
                        Submissions = feedbackLookup.ContainsKey(qr.Id)
                            ? feedback.FeedbackSubmitted
                            : 0,
                        OptIns = feedbackLookup.ContainsKey(qr.Id)
                            ? feedback.MarketingOptIns
                            : 0,
                        Claims = 0,
                        LastScanAtUtc = lastScanLookup.ContainsKey(qr.Id)
                            ? lastScanAt
                            : null,
                    };
                })
                .ToList();
        }

        internal static string ResolveName(QrCode qr)
        {
            if (qr.QrType == QrType.DigitalGuestLink
                && !string.IsNullOrWhiteSpace(qr.LinkName))
            {
                return qr.LinkName.Trim();
            }

            return FeedbackQrSourceMapping.ToDisplay(qr) ?? "QR code";
        }

        internal static string ResolvePlacement(QrCode qr)
        {
            if (qr.QrType == QrType.DigitalGuestLink
                && qr.Channel != null)
            {
                return FormatChannelLabel(qr.Channel.Value.ToString());
            }

            return FeedbackQrSourceMapping.ToDisplay(qr) ?? "QR code";
        }

        private static string FormatStatusLabel(QrCodeStatus status) =>
            status switch
            {
                QrCodeStatus.Active => "Active",
                QrCodeStatus.Paused => "Paused",
                QrCodeStatus.Archived => "Archived",
                _ => status.ToString(),
            };

        private static string FormatChannelLabel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return "Digital guest link";
            }

            return channel.Trim() switch
            {
                "SocialMedia" => "Social media",
                "WhatsApp" => "WhatsApp",
                "Email" => "Email",
                "Website" => "Website",
                _ => channel.Trim(),
            };
        }
    }
}

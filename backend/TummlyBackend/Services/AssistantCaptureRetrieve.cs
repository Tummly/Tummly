using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AssistantCaptureRetrieve : IAssistantCaptureRetrieve
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptureWindowedEngagementAggregate _engagement;

        public AssistantCaptureRetrieve(
            ApplicationDbContext context,
            CaptureWindowedEngagementAggregate engagement
        )
        {
            _context = context;
            _engagement = engagement;
        }

        public async Task<AssistantCaptureRetrieveResult> RetrieveAsync(
            int ownedLocationId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var qrCodes = await _context.QrCodes
                    .AsNoTracking()
                    .Where(qr =>
                        qr.RestaurantLocationId == ownedLocationId
                        && (qr.Status == QrCodeStatus.Active
                            || qr.Status == QrCodeStatus.Paused)
                        && qr.QrType != QrType.DigitalGuestLink
                    )
                    .OrderBy(qr => qr.QrType)
                    .ThenBy(qr => qr.Id)
                    .Select(qr => new
                    {
                        qr.Id,
                        qr.QrType,
                        qr.Status,
                    })
                    .ToListAsync(cancellationToken);

                var qrCodeIds = qrCodes.Select(qr => qr.Id).ToList();
                var locationIds = new[] { ownedLocationId };
                var span = toUtc - fromUtc;
                var previousFromUtc = fromUtc - span;
                var previousToUtc = fromUtc;

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
                    row => row.QrCodeId,
                    row => row.Count
                );
                var feedbackLookup = windowedFeedback.ToDictionary(
                    row => row.QrCodeId
                );

                var qrScans = windowedScans.Sum(row => row.Count);
                var feedbackSubmitted = windowedFeedback.Sum(row => row.FeedbackSubmitted);
                var marketingOptIns = windowedFeedback.Sum(row => row.MarketingOptIns);

                var qrScansPrevious = await _engagement.CountScansAsync(
                    locationIds,
                    qrCodeIds,
                    previousFromUtc,
                    previousToUtc
                );
                var feedbackSubmittedPrevious = await _engagement.CountFeedbackAsync(
                    locationIds,
                    qrCodeIds,
                    previousFromUtc,
                    previousToUtc,
                    marketingOptInOnly: false
                );
                var marketingOptInsPrevious = await _engagement.CountFeedbackAsync(
                    locationIds,
                    qrCodeIds,
                    previousFromUtc,
                    previousToUtc,
                    marketingOptInOnly: true
                );

                var qrRows = qrCodes
                    .Select(qr =>
                    {
                        feedbackLookup.TryGetValue(qr.Id, out var feedback);
                        return new AssistantCaptureQrRow(
                            qr.Id,
                            qr.QrType.ToString(),
                            qr.Status.ToString(),
                            scanLookup.GetValueOrDefault(qr.Id),
                            feedback.FeedbackSubmitted,
                            feedback.MarketingOptIns
                        );
                    })
                    .ToList();

                return new AssistantCaptureRetrieveResult.Ok(
                    new AssistantCaptureEvidence(
                        qrScans,
                        qrScansPrevious,
                        feedbackSubmitted,
                        feedbackSubmittedPrevious,
                        marketingOptIns,
                        marketingOptInsPrevious,
                        qrRows
                    )
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new AssistantCaptureRetrieveResult.Failed();
            }
        }
    }
}

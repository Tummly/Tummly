using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Refusal rules for guest replies. Callers must hold the guest reply lock
    /// (<see cref="Interfaces.IGuestReplyLock"/>) while they run these checks and
    /// save, so a repeat cannot pass both checks at once.
    /// </summary>
    public static class GuestReplyRules
    {
        public static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);

        private static readonly Regex WhitespaceRun = new(
            @"\s+",
            RegexOptions.CultureInvariant
        );

        // Appended to SMS Offer bodies by FeedbackRecoveryOffersService.
        private static readonly Regex ClaimCodeSuffix = new(
            @" Offer claim code: \S+$",
            RegexOptions.CultureInvariant
        );

        /// <summary>Trim, then collapse every whitespace run to one space. Case is kept.</summary>
        public static string NormalizeBody(string body)
            => WhitespaceRun.Replace(body.Trim(), " ");

        /// <summary>
        /// Body of a stored row in the form it had before any claim-code text was
        /// appended, so an operator repeat compares equal.
        /// </summary>
        public static string ComparableStoredBody(
            string storedBody,
            FeedbackRecoveryIntent intent,
            FeedbackGuestResponseChannel channel
        )
        {
            var normalized = NormalizeBody(storedBody);
            if (
                intent == FeedbackRecoveryIntent.RespondWithRecoveryOffer
                && channel == FeedbackGuestResponseChannel.Sms
            )
            {
                normalized = ClaimCodeSuffix.Replace(normalized, string.Empty);
            }

            return normalized;
        }

        /// <summary>
        /// Refuses when the same text was already sent to this Feedback on this
        /// channel within <see cref="DuplicateWindow"/>.
        /// </summary>
        public static async Task EnsureNotDuplicateAsync(
            ApplicationDbContext context,
            int feedbackId,
            FeedbackGuestResponseChannel channel,
            string body,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            var since = nowUtc - DuplicateWindow;
            var recent = await context.FeedbackGuestResponses
                .AsNoTracking()
                .Where(row =>
                    row.FeedbackId == feedbackId
                    && row.Channel == channel
                    && row.CreatedAt >= since
                )
                .Select(row => new { row.Body, row.Intent })
                .ToListAsync(cancellationToken);

            var wanted = NormalizeBody(body);
            if (
                recent.Any(row =>
                    ComparableStoredBody(row.Body, row.Intent, channel) == wanted
                )
            )
            {
                throw new GuestReplyDuplicateException();
            }
        }

        /// <summary>
        /// Refuses when a Recovery Offer for this Feedback is still open: not
        /// redeemed, not cancelled or voided, and not expired.
        /// </summary>
        public static async Task EnsureNoOpenRecoveryOfferAsync(
            ApplicationDbContext context,
            int feedbackId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            var openClaimCode = await context.OfferIssues
                .AsNoTracking()
                .Where(issue =>
                    issue.Source == OfferIssueSources.Recovery
                    && issue.FeedbackId == feedbackId
                    && issue.RedeemedAtUtc == null
                    && issue.CancelledAtUtc == null
                    && issue.ExpiryAtUtc > nowUtc
                )
                .Select(issue => issue.ClaimCode)
                .FirstOrDefaultAsync(cancellationToken);

            if (openClaimCode != null)
            {
                throw new RecoveryOfferOpenException(openClaimCode);
            }
        }
    }
}

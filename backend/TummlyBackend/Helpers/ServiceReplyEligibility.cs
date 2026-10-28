using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Outcome of the shared compose-time and send-time gate for Feedback guest
    /// replies and recovery-Offer replies.
    /// </summary>
    public enum ServiceReplyDecision
    {
        Allowed,

        /// <summary>Restaurant or guest FeedbackFollowUp is off, or the guest was erased.</summary>
        NotEligible,

        /// <summary>SMS STOP on this sender: Twilio enforces it as a provider restriction.</summary>
        SmsProviderRestriction,

        /// <summary>Permanent Email hard bounce on this destination.</summary>
        EmailDeliveryFailure,
    }

    /// <summary>
    /// Shared compose-time and send-time gate for Feedback guest replies and
    /// recovery-Offer replies: restaurant FeedbackFollowUp Enabled, guest
    /// FeedbackFollowUp Granted (effective state, legacy rollup aware), contact
    /// resolves to the channel's ContactType, and no blocking suppression for
    /// the reply channel at this restaurant. Marketing permission is not
    /// checked here: an Email marketing unsubscribe does not stop service email.
    /// </summary>
    public static class ServiceReplyEligibility
    {
        public const string NotAllowedMessage =
            "Guest has not granted feedback follow-up permission.";

        /// <summary>
        /// Operator-facing status for a refused reply. It names no guest data.
        /// </summary>
        public const string EmailDeliveryUnavailableMessage =
            "Email delivery unavailable. Use a verified alternative contact method.";

        public static async Task<bool> IsAllowedAsync(
            ApplicationDbContext context,
            IGuestContactSuppressionService suppressions,
            Feedback feedback,
            FeedbackGuestResponseChannel channel,
            CancellationToken cancellationToken
        )
        {
            var decision = await EvaluateAsync(
                context,
                suppressions,
                feedback,
                channel,
                cancellationToken
            );
            return decision == ServiceReplyDecision.Allowed;
        }

        /// <summary>
        /// Operator-facing refusal text for a decision. Null when allowed.
        /// </summary>
        public static string? RefusalMessage(ServiceReplyDecision decision) =>
            decision switch
            {
                ServiceReplyDecision.Allowed => null,
                ServiceReplyDecision.EmailDeliveryFailure =>
                    EmailDeliveryUnavailableMessage,
                _ => NotAllowedMessage,
            };

        public static async Task<ServiceReplyDecision> EvaluateAsync(
            ApplicationDbContext context,
            IGuestContactSuppressionService suppressions,
            Feedback feedback,
            FeedbackGuestResponseChannel channel,
            CancellationToken cancellationToken
        )
        {
            if (feedback.LocationGuestId is not int locationGuestId)
            {
                return ServiceReplyDecision.NotEligible;
            }

            if (
                !GuestFormContactValidate.TryResolve(
                    feedback.GuestContact,
                    out var contactType
                )
            )
            {
                return ServiceReplyDecision.NotEligible;
            }

            var expectedContactType = channel switch
            {
                FeedbackGuestResponseChannel.Email => ContactType.Email,
                FeedbackGuestResponseChannel.Sms => ContactType.Phone,
                _ => ContactType.Unknown,
            };

            if (contactType != expectedContactType)
            {
                return ServiceReplyDecision.NotEligible;
            }

            var guest = await context.LocationGuests
                .AsNoTracking()
                .Where(g => g.Id == locationGuestId)
                .Select(g => new
                {
                    g.MarketingPreference,
                    RestaurantId = g.RestaurantLocation!.RestaurantId,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (guest == null || guest.RestaurantId == 0)
            {
                return ServiceReplyDecision.NotEligible;
            }

            var restaurant = await context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Id == guest.RestaurantId,
                    cancellationToken
                );

            if (restaurant == null)
            {
                return ServiceReplyDecision.NotEligible;
            }

            var ledgerStates = await new LocationGuestPermissionLedgerService(
                context
            ).GetCurrentStatesAsync(locationGuestId, cancellationToken);

            var states = LocationGuestChannelPermissionGate.ResolveEffectiveStates(
                guest.MarketingPreference,
                ledgerStates
            );

            if (
                !LocationGuestChannelPermissionGate.CanSendFeedbackFollowUp(
                    restaurant,
                    states
                )
            )
            {
                return ServiceReplyDecision.NotEligible;
            }

            if (
                !GuestContactKey.TryCreate(
                    feedback.GuestContact,
                    contactType,
                    out var contactKey
                )
            )
            {
                return ServiceReplyDecision.NotEligible;
            }

            var suppressionRows = await suppressions.FindAsync(
                restaurant.Id,
                contactKey,
                cancellationToken
            );

            return ClassifySuppressions(suppressionRows, channel);
        }

        /// <summary>
        /// Maps the suppression rows for one contact to a reply decision.
        /// <list type="bullet">
        /// <item>FeedbackFollowUp rows and erasure rows (guest delete, retention
        /// purge) block every channel: case-specific or erased.</item>
        /// <item>Email: an <c>EmailDelivery</c> row (hard bounce) blocks this
        /// destination. Marketing kinds (unsubscribe, complaint) do not.</item>
        /// <item>SMS: a <c>SmsMarketing</c> row from STOP blocks service SMS on
        /// this sender, because Twilio enforces STOP as a provider restriction.</item>
        /// </list>
        /// </summary>
        public static ServiceReplyDecision ClassifySuppressions(
            IEnumerable<GuestContactSuppression> rows,
            FeedbackGuestResponseChannel channel
        )
        {
            var list = rows.ToList();

            if (
                list.Any(row =>
                    row.PermissionKind == LocationGuestPermissionKind.FeedbackFollowUp
                    || row.Source is GuestContactSuppressionSources.GuestDelete
                        or GuestContactSuppressionSources.RetentionPurge
                )
            )
            {
                return ServiceReplyDecision.NotEligible;
            }

            if (channel == FeedbackGuestResponseChannel.Email)
            {
                return list.Any(row =>
                    row.PermissionKind == LocationGuestPermissionKind.EmailDelivery
                )
                    ? ServiceReplyDecision.EmailDeliveryFailure
                    : ServiceReplyDecision.Allowed;
            }

            return list.Any(row =>
                row.PermissionKind == LocationGuestPermissionKind.SmsMarketing
                && row.Source == GuestContactSuppressionSources.SmsStop
            )
                ? ServiceReplyDecision.SmsProviderRestriction
                : ServiceReplyDecision.Allowed;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared compose-time and send-time gate for Feedback guest replies and
    /// recovery-Offer replies: restaurant FeedbackFollowUp Enabled, guest
    /// FeedbackFollowUp Granted (effective state, legacy rollup aware), contact
    /// resolves to the channel's ContactType, and no suppression for the reply
    /// channel at this restaurant. Marketing permission is not checked here.
    /// </summary>
    public static class ServiceReplyEligibility
    {
        public const string NotAllowedMessage =
            "Guest has not granted feedback follow-up permission.";

        public static async Task<bool> IsAllowedAsync(
            ApplicationDbContext context,
            IGuestContactSuppressionService suppressions,
            Feedback feedback,
            FeedbackGuestResponseChannel channel,
            CancellationToken cancellationToken
        )
        {
            if (feedback.LocationGuestId is not int locationGuestId)
            {
                return false;
            }

            if (
                !GuestFormContactValidate.TryResolve(
                    feedback.GuestContact,
                    out var contactType
                )
            )
            {
                return false;
            }

            var expectedContactType = channel switch
            {
                FeedbackGuestResponseChannel.Email => ContactType.Email,
                FeedbackGuestResponseChannel.Sms => ContactType.Phone,
                _ => ContactType.Unknown,
            };

            if (contactType != expectedContactType)
            {
                return false;
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
                return false;
            }

            var restaurant = await context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Id == guest.RestaurantId,
                    cancellationToken
                );

            if (restaurant == null)
            {
                return false;
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
                return false;
            }

            if (
                !GuestContactKey.TryCreate(
                    feedback.GuestContact,
                    contactType,
                    out var contactKey
                )
            )
            {
                return false;
            }

            var suppressionRows = await suppressions.FindAsync(
                restaurant.Id,
                contactKey,
                cancellationToken
            );

            return !IsSuppressedForChannel(suppressionRows, channel);
        }

        /// <summary>
        /// A suppression blocks a reply when it is for this channel's marketing
        /// kind (email unsubscribe or SMS STOP), or for FeedbackFollowUp, or is
        /// an erasure (guest delete or retention purge) on any channel.
        /// </summary>
        public static bool IsSuppressedForChannel(
            IEnumerable<GuestContactSuppression> rows,
            FeedbackGuestResponseChannel channel
        )
        {
            var channelKind = channel == FeedbackGuestResponseChannel.Email
                ? LocationGuestPermissionKind.EmailMarketing
                : LocationGuestPermissionKind.SmsMarketing;

            return rows.Any(row =>
                row.PermissionKind == channelKind
                || row.PermissionKind == LocationGuestPermissionKind.FeedbackFollowUp
                || row.Source is GuestContactSuppressionSources.GuestDelete
                    or GuestContactSuppressionSources.RetentionPurge
            );
        }
    }
}

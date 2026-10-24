using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared compose-time gate for Feedback guest replies and recovery-Offer
    /// replies: restaurant FeedbackFollowUp Enabled, guest FeedbackFollowUp
    /// Granted (effective state, legacy rollup aware), and contact resolves to
    /// the channel's ContactType. WS2c adds bounce/complaint suppression here.
    /// </summary>
    public static class ServiceReplyEligibility
    {
        public const string NotAllowedMessage =
            "Guest has not granted feedback follow-up permission.";

        public static async Task<bool> IsAllowedAsync(
            ApplicationDbContext context,
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

            return LocationGuestChannelPermissionGate.CanSendFeedbackFollowUp(
                restaurant,
                states
            );
        }
    }
}

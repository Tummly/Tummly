using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IGuestInitiatedMarketingWithdrawService
    {
        Task<GuestInitiatedWithdrawResult> WithdrawForLocationGuestAsync(
            int locationGuestId,
            int restaurantId,
            LocationGuestPermissionKind kind,
            string ledgerSource,
            CancellationToken cancellationToken = default
        );

        /// <param name="suppressionSource">
        /// Source stamped on the staged suppression when no guest matches.
        /// Null uses the channel default (email unsubscribe / SMS STOP).
        /// </param>
        Task<GuestInitiatedWithdrawResult> WithdrawForRestaurantContactAsync(
            int restaurantId,
            string normalizedEmailOrE164,
            bool isEmail,
            LocationGuestPermissionKind kind,
            string ledgerSource,
            CancellationToken cancellationToken = default,
            string? suppressionSource = null
        );
    }

    public sealed record GuestInitiatedWithdrawResult(
        int GuestsTouched,
        int WithdrawalsWritten,
        bool ActivityEmitted
    );
}

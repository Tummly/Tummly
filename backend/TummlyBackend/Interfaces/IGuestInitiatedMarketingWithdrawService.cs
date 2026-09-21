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

        Task<GuestInitiatedWithdrawResult> WithdrawForRestaurantContactAsync(
            int restaurantId,
            string normalizedEmailOrE164,
            bool isEmail,
            LocationGuestPermissionKind kind,
            string ledgerSource,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record GuestInitiatedWithdrawResult(
        int GuestsTouched,
        int WithdrawalsWritten,
        bool ActivityEmitted
    );
}

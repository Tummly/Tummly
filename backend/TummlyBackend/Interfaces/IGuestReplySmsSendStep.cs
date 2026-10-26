using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// SMS steps shared by every guest reply that sends SMS: plain guest
    /// responses, Recovery Offers and Respond-and-Record. Callers hold the guest
    /// reply lock and run their own duplicate and open-Offer checks first.
    /// </summary>
    public interface IGuestReplySmsSendStep
    {
        /// <summary>Throws <see cref="Services.RecoverySmsBillingUnavailableException"/> when billing is not live.</summary>
        void EnsureBillingLive();

        /// <summary>Throws <see cref="Helpers.OperatorBillingLockedException"/> when the operator account is locked.</summary>
        Task EnsureOperatorBillingAllowsSendAsync(
            int locationId,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Reserves credits for <paramref name="body"/> (or reuses an open hold for
        /// <paramref name="idempotencyKey"/>). Throws
        /// <see cref="Services.RecoverySmsCreditRefusedException"/> when refused,
        /// and saves nothing else on the shared context before that.
        /// </summary>
        Task<GuestReplySmsReservation> ReserveAsync(
            Feedback feedback,
            string idempotencyKey,
            string body,
            CancellationToken cancellationToken
        );

        Task<RecoveryGuestSmsDeliveryResult> SendAsync(
            Feedback feedback,
            string body,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Settles the accepted units, then releases the unused hold. Returns false
        /// when settle is refused; the caller then calls <see cref="ReleaseFailedAsync"/>.
        /// </summary>
        Task<bool> SettleAcceptedAsync(
            GuestReplySmsReservation reservation,
            RecoveryGuestSmsDeliveryResult.Accepted accepted,
            CancellationToken cancellationToken
        );

        /// <summary>Releases the whole hold and clears its open row. Saves.</summary>
        Task ReleaseFailedAsync(
            GuestReplySmsReservation reservation,
            CancellationToken cancellationToken
        );

        /// <summary>Stores the reservation ref, provider SID and delivery state on the reply row.</summary>
        void ApplyAccepted(
            FeedbackGuestResponse row,
            GuestReplySmsReservation reservation,
            RecoveryGuestSmsDeliveryResult.Accepted accepted,
            DateTime atUtc
        );

        /// <summary>Marks the idempotency row completed (or drops the open hold when there is no key). Caller saves.</summary>
        Task CompleteAsync(
            GuestReplySmsReservation reservation,
            FeedbackGuestResponse row,
            CancellationToken cancellationToken
        );
    }

    public sealed record GuestReplySmsReservation(
        int FeedbackId,
        int RestaurantId,
        string IdempotencyKey,
        string ReservationRef
    );
}

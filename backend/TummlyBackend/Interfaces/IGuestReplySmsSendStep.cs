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
        /// <see cref="Services.RecoverySmsCreditRefusedException"/> when refused.
        /// </summary>
        Task<GuestReplySmsReservation> ReserveAsync(
            Feedback feedback,
            string idempotencyKey,
            string body,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Sends the text, then settles the credits when the provider accepted it.
        /// A failed send releases the hold. A provider exception leaves the hold
        /// open for the sweeper and reports <see cref="GuestReplySmsOutcomeKind.Unknown"/>.
        /// A settle failure after acceptance is logged and never thrown.
        /// </summary>
        Task<GuestReplySmsOutcome> SendAndSettleAsync(
            GuestReplySmsReservation reservation,
            Feedback feedback,
            string body,
            CancellationToken cancellationToken
        );

        /// <summary>Stores the reservation ref, provider SID and delivery state on the reply row.</summary>
        void ApplyAccepted(
            FeedbackGuestResponse row,
            GuestReplySmsReservation reservation,
            RecoveryGuestSmsDeliveryResult.Accepted accepted,
            DateTime atUtc
        );

        /// <summary>Stores the reservation ref and sets the reply to Unavailable with no SID.</summary>
        void ApplyUnknownOutcome(
            FeedbackGuestResponse row,
            GuestReplySmsReservation reservation,
            DateTime atUtc
        );

        /// <summary>Marks the idempotency row completed (or drops the open hold when there is no key). Caller saves.</summary>
        Task CompleteAsync(
            GuestReplySmsReservation reservation,
            FeedbackGuestResponse row,
            CancellationToken cancellationToken
        );

        /// <summary>Releases the whole hold and clears its open row. Saves.</summary>
        Task ReleaseFailedAsync(
            GuestReplySmsReservation reservation,
            CancellationToken cancellationToken
        );
    }

    public sealed record GuestReplySmsReservation(
        int FeedbackId,
        int RestaurantId,
        string IdempotencyKey,
        string ReservationRef
    );

    public enum GuestReplySmsOutcomeKind
    {
        /// <summary>The provider accepted the text.</summary>
        Accepted,

        /// <summary>The provider refused the text. Nothing was sent.</summary>
        Failed,

        /// <summary>The provider call threw. The text may have been sent.</summary>
        Unknown,
    }

    /// <param name="Accepted">Set when <see cref="Kind"/> is Accepted.</param>
    /// <param name="Settled">
    /// True when the accepted units were settled. When false the hold stays open
    /// for the sweeper and the idempotency record must not be completed.
    /// </param>
    public sealed record GuestReplySmsOutcome(
        GuestReplySmsOutcomeKind Kind,
        RecoveryGuestSmsDeliveryResult.Accepted? Accepted,
        bool Settled
    );
}

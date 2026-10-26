namespace TummlyBackend.Services
{
    /// <summary>
    /// An identical reply was already sent to the same guest on the same
    /// channel within the last 10 minutes. Nothing is saved or sent.
    /// </summary>
    public sealed class GuestReplyDuplicateException : Exception
    {
        public const string ErrorCode = "duplicate_reply";

        public GuestReplyDuplicateException()
            : base("This reply was already sent to the guest.")
        {
        }

        public string Code => ErrorCode;
    }

    /// <summary>
    /// A recovery Offer for this Feedback is still open (not redeemed, not
    /// voided, not expired). A second one is refused.
    /// </summary>
    public sealed class RecoveryOfferOpenException : Exception
    {
        public const string ErrorCode = "recovery_offer_open";

        public RecoveryOfferOpenException(string claimCode)
            : base(
                $"A recovery Offer for this feedback is still open ({claimCode}). Send a reply without an Offer, or void the open Offer first."
            )
        {
            ClaimCode = claimCode;
        }

        public string ClaimCode { get; }

        public string Code => ErrorCode;
    }

    /// <summary>
    /// The per-Feedback guest reply lock was not granted in time.
    /// </summary>
    public sealed class GuestReplyLockUnavailableException : Exception
    {
        public GuestReplyLockUnavailableException()
            : base("Please try again.")
        {
        }
    }
}

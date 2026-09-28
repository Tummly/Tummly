namespace TummlyBackend.DTOs.Capture
{
    public sealed class SetCaptureThankYouOfferRequest
    {
        /// <summary>Catalog offer id to attach, or null to clear.</summary>
        public int? OfferId { get; set; }
    }

    /// <summary>
    /// Live thank-you attach as shown on Capture / Digital guest link.
    /// Non-Active stored FKs are returned as empty (not attached).
    /// </summary>
    public sealed class CaptureThankYouOfferDto
    {
        public int? ThankYouOfferId { get; init; }

        public string? ThankYouOfferTitle { get; init; }

        /// <summary>
        /// True when a live Active thank-you attach is returned.
        /// Always false when <see cref="ThankYouOfferId"/> is null.
        /// </summary>
        public bool ThankYouOfferLive { get; init; }
    }

    public abstract record CaptureThankYouOfferSetResult
    {
        public sealed record Ok(CaptureThankYouOfferDto Value)
            : CaptureThankYouOfferSetResult;

        public sealed record LocationNotFound : CaptureThankYouOfferSetResult;

        public sealed record InvalidOffer(string Message)
            : CaptureThankYouOfferSetResult;

        public sealed record CapReached(int Cap, int Current)
            : CaptureThankYouOfferSetResult;

        public sealed record FailClosed : CaptureThankYouOfferSetResult;
    }
}

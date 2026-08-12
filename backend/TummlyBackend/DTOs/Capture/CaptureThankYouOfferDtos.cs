namespace TummlyBackend.DTOs.Capture
{
    public sealed class SetCaptureThankYouOfferRequest
    {
        /// <summary>Catalog offer id to attach, or null to clear.</summary>
        public int? OfferId { get; set; }
    }

    /// <summary>
    /// Persisted thank-you attach as shown on Capture (and for issue gating).
    /// </summary>
    public sealed class CaptureThankYouOfferDto
    {
        public int? ThankYouOfferId { get; init; }

        public string? ThankYouOfferTitle { get; init; }

        /// <summary>
        /// True when the stored id is attachable Active for this location.
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
    }
}

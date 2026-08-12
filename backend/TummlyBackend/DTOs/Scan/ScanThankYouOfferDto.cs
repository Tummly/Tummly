namespace TummlyBackend.DTOs.Scan
{
    /// <summary>
    /// Issued Guest form thank-you offer painted after successful submit.
    /// Null on the submit response when issue is skipped.
    /// </summary>
    public sealed class ScanThankYouOfferDto
    {
        public required string Title { get; init; }

        public required string Description { get; init; }

        public required string ClaimCode { get; init; }

        public required string ExpiryLabel { get; init; }
    }
}

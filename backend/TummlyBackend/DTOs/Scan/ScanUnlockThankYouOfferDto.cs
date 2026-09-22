namespace TummlyBackend.DTOs.Scan
{
    /// <summary>
    /// Live thank-you offer available after submit when marketing consent
    /// was not granted — Guest unlock screen (Figma 6723:1094).
    /// </summary>
    public sealed class ScanUnlockThankYouOfferDto
    {
        public required string Title { get; init; }

        /// <summary><c>email</c> or <c>sms</c>.</summary>
        public required string Channel { get; init; }

        public required string RestaurantName { get; init; }

        public required string LocationName { get; init; }

        /// <summary>Signed handoff for POST unlock.</summary>
        public required string UnlockToken { get; init; }
    }

    public sealed class UnlockThankYouOfferRequest
    {
        public required string UnlockToken { get; init; }
    }
}

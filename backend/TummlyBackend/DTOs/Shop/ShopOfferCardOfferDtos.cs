namespace TummlyBackend.DTOs.Shop
{
    public sealed class SetShopOfferCardOfferRequest
    {
        /// <summary>Catalog offer id to attach, or null to clear.</summary>
        public int? OfferId { get; set; }
    }

    /// <summary>
    /// Offer Card print attach for Shop setup + print materials.
    /// Setup is complete when <see cref="OfferCardOfferId"/> is set.
    /// </summary>
    public sealed class ShopOfferCardOfferDto
    {
        public int? OfferCardOfferId { get; init; }

        public string? OfferCardOfferTitle { get; init; }

        /// <summary>
        /// True when the attached offer is currently Active/attachable-live.
        /// </summary>
        public bool OfferCardOfferLive { get; init; }
    }

    public abstract record ShopOfferCardOfferSetResult
    {
        public sealed record Ok(ShopOfferCardOfferDto Value)
            : ShopOfferCardOfferSetResult;

        public sealed record LocationNotFound : ShopOfferCardOfferSetResult;

        public sealed record InvalidOffer(string Message)
            : ShopOfferCardOfferSetResult;

        public sealed record CapReached(int Cap, int Current)
            : ShopOfferCardOfferSetResult;

        public sealed record FailClosed : ShopOfferCardOfferSetResult;
    }
}

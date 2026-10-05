namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Operator tried to add/order Offer Cards before the location Offer Card
    /// print offer was set up.
    /// </summary>
    public sealed class OfferCardOfferRequiredException : InvalidOperationException
    {
        public const string ErrorCode = "offer_card_offer_required";

        public OfferCardOfferRequiredException()
            : base(
                "Set up your card offer before ordering Offer Cards."
            )
        {
        }
    }
}

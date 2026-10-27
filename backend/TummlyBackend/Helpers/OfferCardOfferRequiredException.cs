namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Operator tried to add/order Guest Cards before the location card offer
    /// was set up.
    /// </summary>
    public sealed class OfferCardOfferRequiredException : InvalidOperationException
    {
        public const string ErrorCode = "offer_card_offer_required";

        /// <summary>The one message every Shop surface shows for this error.</summary>
        public const string ErrorMessage =
            "Set up your card offer before ordering Guest Cards.";

        public OfferCardOfferRequiredException()
            : base(ErrorMessage)
        {
        }
    }
}

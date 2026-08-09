namespace TummlyBackend.Models
{
    /// <summary>
    /// Reusable Offers catalog benefit type (Campaign create-and-select).
    /// Wire values match Recovery offer form patterns; entity is not FeedbackRecoveryOffer.
    /// </summary>
    public enum CatalogOfferType
    {
        PercentageDiscount = 0,
        FixedDiscount = 1,
        FreeItem = 2,
        ReplacementItem = 3,
    }

    public enum CatalogOfferPurchaseRequirement
    {
        NoPurchaseRequired = 0,
        WithAnyPurchase = 1,
        WithMinimumSpend = 2,
    }

    public enum CatalogOfferValidity
    {
        Days7AfterIssue = 0,
        Days14AfterIssue = 1,
        Days30AfterIssue = 2,
        ChooseExpiryDate = 3,
    }
}

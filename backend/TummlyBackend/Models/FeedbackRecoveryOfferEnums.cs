namespace TummlyBackend.Models
{
    /// <summary>
    /// One-off recovery offer type created in the Respond with a recovery offer wizard.
    /// </summary>
    public enum FeedbackRecoveryOfferType
    {
        PercentageDiscount = 0,
        FixedDiscount = 1,
        FreeItem = 2,
        ReplacementItem = 3,
    }

    public enum FeedbackRecoveryOfferPurchaseRequirement
    {
        NoPurchaseRequired = 0,
        WithAnyPurchase = 1,
        WithMinimumSpend = 2,
    }

    public enum FeedbackRecoveryOfferValidity
    {
        Days7AfterIssue = 0,
        Days14AfterIssue = 1,
        Days30AfterIssue = 2,
        ChooseExpiryDate = 3,
    }
}

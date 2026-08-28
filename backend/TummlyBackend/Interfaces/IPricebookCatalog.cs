namespace TummlyBackend.Interfaces
{
    using TummlyBackend.Billing.Pricebook;
    using TummlyBackend.DTOs.BillingCredits;

    public interface IPricebookCatalog
    {
        string CurrentPricebookId { get; }

        PricebookSnapshot GetRequired(string pricebookId);

        BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available);

        string FormatPlanPriceNet(PricebookPlan plan, string? billingCycle);

        string FormatIncludedCreditsLabel(
            PricebookPlan plan,
            string channel
        );
    }
}

using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class CatalogOfferMapping
    {
        public static string ToWireOfferType(CatalogOfferType type)
            => type switch
            {
                CatalogOfferType.PercentageDiscount => "percentage_discount",
                CatalogOfferType.FixedDiscount => "fixed_discount",
                CatalogOfferType.FreeItem => "free_item",
                CatalogOfferType.ReplacementItem => "replacement_item",
                _ => "percentage_discount",
            };

        public static bool TryParseOfferType(string? wire, out CatalogOfferType type)
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "percentage_discount":
                    type = CatalogOfferType.PercentageDiscount;
                    return true;
                case "fixed_discount":
                    type = CatalogOfferType.FixedDiscount;
                    return true;
                case "free_item":
                    type = CatalogOfferType.FreeItem;
                    return true;
                case "replacement_item":
                    type = CatalogOfferType.ReplacementItem;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }

        public static string ToWireValidity(CatalogOfferValidity validity)
            => validity switch
            {
                CatalogOfferValidity.Days7AfterIssue => "7_days_after_issue",
                CatalogOfferValidity.Days14AfterIssue => "14_days_after_issue",
                CatalogOfferValidity.Days30AfterIssue => "30_days_after_issue",
                CatalogOfferValidity.ChooseExpiryDate => "choose_expiry_date",
                _ => "30_days_after_issue",
            };

        public static bool TryParseValidity(string? wire, out CatalogOfferValidity validity)
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "7_days_after_issue":
                    validity = CatalogOfferValidity.Days7AfterIssue;
                    return true;
                case "14_days_after_issue":
                    validity = CatalogOfferValidity.Days14AfterIssue;
                    return true;
                case "30_days_after_issue":
                    validity = CatalogOfferValidity.Days30AfterIssue;
                    return true;
                case "choose_expiry_date":
                    validity = CatalogOfferValidity.ChooseExpiryDate;
                    return true;
                default:
                    validity = default;
                    return false;
            }
        }

        public static string? ToWirePurchaseRequirement(
            CatalogOfferPurchaseRequirement? requirement
        )
            => requirement switch
            {
                CatalogOfferPurchaseRequirement.NoPurchaseRequired =>
                    "no_purchase_required",
                CatalogOfferPurchaseRequirement.WithAnyPurchase =>
                    "with_any_purchase",
                CatalogOfferPurchaseRequirement.WithMinimumSpend =>
                    "with_minimum_spend",
                _ => null,
            };

        public static bool TryParsePurchaseRequirement(
            string? wire,
            out CatalogOfferPurchaseRequirement requirement
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "no_purchase_required":
                    requirement = CatalogOfferPurchaseRequirement.NoPurchaseRequired;
                    return true;
                case "with_any_purchase":
                    requirement = CatalogOfferPurchaseRequirement.WithAnyPurchase;
                    return true;
                case "with_minimum_spend":
                    requirement = CatalogOfferPurchaseRequirement.WithMinimumSpend;
                    return true;
                default:
                    requirement = default;
                    return false;
            }
        }
    }
}

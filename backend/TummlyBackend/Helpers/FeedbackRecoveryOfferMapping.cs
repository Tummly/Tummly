using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class FeedbackRecoveryOfferMapping
    {
        public static string ToWireOfferType(FeedbackRecoveryOfferType type)
            => type switch
            {
                FeedbackRecoveryOfferType.PercentageDiscount =>
                    "percentage_discount",
                FeedbackRecoveryOfferType.FixedDiscount => "fixed_discount",
                FeedbackRecoveryOfferType.FreeItem => "free_item",
                FeedbackRecoveryOfferType.ReplacementItem => "replacement_item",
                _ => "percentage_discount",
            };

        public static bool TryParseOfferType(
            string? wire,
            out FeedbackRecoveryOfferType type
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "percentage_discount":
                    type = FeedbackRecoveryOfferType.PercentageDiscount;
                    return true;
                case "fixed_discount":
                    type = FeedbackRecoveryOfferType.FixedDiscount;
                    return true;
                case "free_item":
                    type = FeedbackRecoveryOfferType.FreeItem;
                    return true;
                case "replacement_item":
                    type = FeedbackRecoveryOfferType.ReplacementItem;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }

        public static string ToWireValidity(FeedbackRecoveryOfferValidity validity)
            => validity switch
            {
                FeedbackRecoveryOfferValidity.Days7AfterIssue =>
                    "7_days_after_issue",
                FeedbackRecoveryOfferValidity.Days14AfterIssue =>
                    "14_days_after_issue",
                FeedbackRecoveryOfferValidity.Days30AfterIssue =>
                    "30_days_after_issue",
                FeedbackRecoveryOfferValidity.ChooseExpiryDate =>
                    "choose_expiry_date",
                _ => "30_days_after_issue",
            };

        public static bool TryParseValidity(
            string? wire,
            out FeedbackRecoveryOfferValidity validity
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "7_days_after_issue":
                    validity = FeedbackRecoveryOfferValidity.Days7AfterIssue;
                    return true;
                case "14_days_after_issue":
                    validity = FeedbackRecoveryOfferValidity.Days14AfterIssue;
                    return true;
                case "30_days_after_issue":
                    validity = FeedbackRecoveryOfferValidity.Days30AfterIssue;
                    return true;
                case "choose_expiry_date":
                    validity = FeedbackRecoveryOfferValidity.ChooseExpiryDate;
                    return true;
                default:
                    validity = default;
                    return false;
            }
        }

        public static string? ToWirePurchaseRequirement(
            FeedbackRecoveryOfferPurchaseRequirement? requirement
        )
            => requirement switch
            {
                FeedbackRecoveryOfferPurchaseRequirement.NoPurchaseRequired =>
                    "no_purchase_required",
                FeedbackRecoveryOfferPurchaseRequirement.WithAnyPurchase =>
                    "with_any_purchase",
                FeedbackRecoveryOfferPurchaseRequirement.WithMinimumSpend =>
                    "with_minimum_spend",
                _ => null,
            };

        public static bool TryParsePurchaseRequirement(
            string? wire,
            out FeedbackRecoveryOfferPurchaseRequirement requirement
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "no_purchase_required":
                    requirement =
                        FeedbackRecoveryOfferPurchaseRequirement.NoPurchaseRequired;
                    return true;
                case "with_any_purchase":
                    requirement =
                        FeedbackRecoveryOfferPurchaseRequirement.WithAnyPurchase;
                    return true;
                case "with_minimum_spend":
                    requirement =
                        FeedbackRecoveryOfferPurchaseRequirement.WithMinimumSpend;
                    return true;
                default:
                    requirement = default;
                    return false;
            }
        }

        public static DateTime ComputeExpiryAt(
            FeedbackRecoveryOfferValidity validity,
            DateTime issuedAtUtc,
            DateOnly? customExpiryDate
        )
        {
            return validity switch
            {
                FeedbackRecoveryOfferValidity.Days7AfterIssue =>
                    issuedAtUtc.AddDays(7),
                FeedbackRecoveryOfferValidity.Days14AfterIssue =>
                    issuedAtUtc.AddDays(14),
                FeedbackRecoveryOfferValidity.Days30AfterIssue =>
                    issuedAtUtc.AddDays(30),
                FeedbackRecoveryOfferValidity.ChooseExpiryDate when
                    customExpiryDate is { } date =>
                    date.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc),
                _ => issuedAtUtc.AddDays(30),
            };
        }

        public static string GenerateRedemptionCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<char> chars = stackalloc char[6];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            }

            return $"TUM-{new string(chars)}";
        }
    }
}

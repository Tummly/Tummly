using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class FeedbackInternalActionMapping
    {
        public static string ToWireCategory(FeedbackInternalActionCategory category)
            => category switch
            {
                FeedbackInternalActionCategory.TeamBriefed => "team_briefed",
                FeedbackInternalActionCategory.OrderOrServiceProcessReviewed =>
                    "order_or_service_process_reviewed",
                FeedbackInternalActionCategory.DeliveryIssueInvestigated =>
                    "delivery_issue_investigated",
                FeedbackInternalActionCategory.ProductQualityChecked =>
                    "product_quality_checked",
                FeedbackInternalActionCategory.CleaningIssueAddressed =>
                    "cleaning_issue_addressed",
                FeedbackInternalActionCategory.StaffFollowUpCompleted =>
                    "staff_follow_up_completed",
                FeedbackInternalActionCategory.OtherAction => "other_action",
                _ => "other_action",
            };

        public static string ToCategoryLabel(FeedbackInternalActionCategory category)
            => category switch
            {
                FeedbackInternalActionCategory.TeamBriefed => "Team briefed",
                FeedbackInternalActionCategory.OrderOrServiceProcessReviewed =>
                    "Order or service process reviewed",
                FeedbackInternalActionCategory.DeliveryIssueInvestigated =>
                    "Delivery issue investigated",
                FeedbackInternalActionCategory.ProductQualityChecked =>
                    "Product quality checked",
                FeedbackInternalActionCategory.CleaningIssueAddressed =>
                    "Cleaning issue addressed",
                FeedbackInternalActionCategory.StaffFollowUpCompleted =>
                    "Staff follow-up completed",
                FeedbackInternalActionCategory.OtherAction => "Other action",
                _ => "Other action",
            };

        public static bool TryParseCategory(
            string? wire,
            out FeedbackInternalActionCategory category
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "team_briefed":
                    category = FeedbackInternalActionCategory.TeamBriefed;
                    return true;
                case "order_or_service_process_reviewed":
                    category =
                        FeedbackInternalActionCategory.OrderOrServiceProcessReviewed;
                    return true;
                case "delivery_issue_investigated":
                    category =
                        FeedbackInternalActionCategory.DeliveryIssueInvestigated;
                    return true;
                case "product_quality_checked":
                    category =
                        FeedbackInternalActionCategory.ProductQualityChecked;
                    return true;
                case "cleaning_issue_addressed":
                    category =
                        FeedbackInternalActionCategory.CleaningIssueAddressed;
                    return true;
                case "staff_follow_up_completed":
                    category =
                        FeedbackInternalActionCategory.StaffFollowUpCompleted;
                    return true;
                case "other_action":
                    category = FeedbackInternalActionCategory.OtherAction;
                    return true;
                default:
                    category = default;
                    return false;
            }
        }

        public static string ToWireIntent(FeedbackRecoveryIntent intent)
            => intent switch
            {
                FeedbackRecoveryIntent.RespondToGuest => "respond_to_guest",
                FeedbackRecoveryIntent.RecordInternalActionOnly =>
                    "record_internal_action_only",
                FeedbackRecoveryIntent.RespondAndRecordInternalAction =>
                    "respond_and_record_internal_action",
                FeedbackRecoveryIntent.RespondWithRecoveryOffer =>
                    "respond_with_recovery_offer",
                _ => "respond_to_guest",
            };

        public static bool TryParseIntent(
            string? wire,
            out FeedbackRecoveryIntent intent
        )
        {
            switch (wire?.Trim().ToLowerInvariant())
            {
                case "respond_to_guest":
                    intent = FeedbackRecoveryIntent.RespondToGuest;
                    return true;
                case "record_internal_action_only":
                    intent = FeedbackRecoveryIntent.RecordInternalActionOnly;
                    return true;
                case "respond_and_record_internal_action":
                    intent =
                        FeedbackRecoveryIntent.RespondAndRecordInternalAction;
                    return true;
                case "respond_with_recovery_offer":
                    intent = FeedbackRecoveryIntent.RespondWithRecoveryOffer;
                    return true;
                default:
                    intent = default;
                    return false;
            }
        }
    }
}

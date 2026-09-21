namespace TummlyBackend.Models
{
    public class AdminAuditEvent
    {
        public Guid Id { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public string Action { get; set; } = string.Empty;

        public int? ActorAdminUserId { get; set; }

        public string ActorIdentity { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string TargetId { get; set; } = string.Empty;

        public int? RestaurantId { get; set; }

        public string? DetailJson { get; set; }

        public bool Succeeded { get; set; } = true;
    }

    public static class AdminAuditActions
    {
        public const string TrialApprove = "trial.approve";
        public const string TrialDecline = "trial.decline";
        public const string TrialRequestMoreInfo = "trial.request_more_info";
        public const string TrialResendInvite = "trial.resend_invite";
        public const string TrialPurge = "trial.purge";
        public const string OperatorExtendActivation = "operator.extend_activation";
        public const string CreditAdjust = "credit.adjust";
        public const string CreditReverse = "credit.reverse";
        public const string PaymentRefund = "payment.refund";
        public const string ShopProductionStarted = "shop.production_started";
        public const string ShopForceCancel = "shop.force_cancel";
        public const string RetentionGuestPurge = "retention.guest_purge";
    }

    public static class AdminAuditTargetTypes
    {
        public const string TrialRequest = "trial_request";
        public const string OperatorUser = "operator_user";
        public const string Restaurant = "restaurant";
        public const string PaymentOrder = "payment_order";
        public const string ShopOrder = "shop_order";
    }
}

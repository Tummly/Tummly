namespace TummlyBackend.Models
{
    public static class BillingActivityKinds
    {
        public const string SubscriptionCreated = "subscription_created";
        public const string SubscriptionUpgraded = "subscription_upgraded";
        public const string SubscriptionChangeScheduled = "subscription_change_scheduled";
        public const string AdditionalLocationAdded = "additional_location_added";
        public const string AdditionalLocationRemoveScheduled =
            "additional_location_remove_scheduled";
        public const string SubscriptionCancelled = "subscription_cancelled";
        public const string SubscriptionRenewed = "subscription_renewed";
        public const string TopupPurchased = "topup_purchased";
        public const string TopupRefunded = "topup_refunded";
        public const string CreditConsumed = "credit_consumed";
        public const string CreditExpired = "credit_expired";
        public const string ManualCreditAdjusted = "manual_credit_adjusted";
        public const string InvoicePaid = "invoice_paid";
        public const string CreditNoteIssued = "credit_note_issued";
        public const string PaymentMethodUpdated = "payment_method_updated";
        public const string SoftLockEntered = "soft_lock_entered";
        public const string DormantEntered = "dormant_entered";
    }
}

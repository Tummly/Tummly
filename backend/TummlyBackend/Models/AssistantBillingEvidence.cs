namespace TummlyBackend.Models
{
    /// <summary>
    /// Restaurant plan / billing status / credit balances for Assistant retrieve.
    /// Server-owned; never invent Revolut payment success.
    /// </summary>
    public sealed record AssistantBillingEvidence(
        string SubscriptionPlan,
        string BillingStatus,
        int EmailCreditsRemaining,
        int SmsCreditsRemaining,
        int AiCreditsRemaining,
        string? BillingCycle,
        string? RenewalDateLabel,
        bool IsPilot,
        string? ScheduledChangeLine,
        string PlanPriceNet
    )
    {
        public static AssistantBillingEvidence Empty { get; } =
            new(
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                null,
                null,
                false,
                null,
                string.Empty
            );

        public bool IsEmpty =>
            string.IsNullOrEmpty(SubscriptionPlan)
            && string.IsNullOrEmpty(BillingStatus);
    }
}

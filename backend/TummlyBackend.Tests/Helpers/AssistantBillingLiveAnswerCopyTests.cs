using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantBillingLiveAnswerCopyTests
    {
        [Fact]
        public void GroundedFromEvidence_BillingAsk_ReturnsPlanAndCredits()
        {
            var evidence = AssistantRetrievedEvidence.Empty with
            {
                Billing = new AssistantBillingEvidence(
                    SubscriptionPlan: "Growth",
                    BillingStatus: "Active",
                    EmailCreditsRemaining: 100,
                    SmsCreditsRemaining: 20,
                    AiCreditsRemaining: 42,
                    BillingCycle: "monthly",
                    RenewalDateLabel: "Renews 1 Nov 2026",
                    IsPilot: false,
                    ScheduledChangeLine: null,
                    PlanPriceNet: "£99"
                ),
            };

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "What's billing Does my account have at the moment?",
                "Camden",
                "this week",
                evidence
            );

            Assert.Equal(AssistantMessageClass.Grounded, result.Class);
            Assert.Equal("Plan and credits for your account", result.Title);
            Assert.Contains("Growth", result.Body, StringComparison.Ordinal);
            Assert.Contains("Active", result.Body, StringComparison.Ordinal);
            Assert.Contains("42", result.Body, StringComparison.Ordinal);
            Assert.Contains(
                "current account facts",
                result.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "this week",
                result.Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void GroundedFromEvidence_UsageAsk_IncludesUsedThisCycle()
        {
            var evidence = AssistantRetrievedEvidence.Empty with
            {
                Billing = new AssistantBillingEvidence(
                    SubscriptionPlan: "Growth",
                    BillingStatus: "Active",
                    EmailCreditsRemaining: 100,
                    SmsCreditsRemaining: 20,
                    AiCreditsRemaining: 42,
                    BillingCycle: "monthly",
                    RenewalDateLabel: "Renews 1 Nov 2026",
                    IsPilot: false,
                    ScheduledChangeLine: null,
                    PlanPriceNet: "£99",
                    EmailUsedThisCycle: 12,
                    SmsUsedThisCycle: 5,
                    AiUsedThisCycle: 3,
                    UsagePeriodLabel: "1–31 Oct 2026",
                    HasUsageSnapshot: true
                ),
            };

            var result = AssistantLiveAnswerCopy.GroundedFromEvidence(
                "How many credits have we used this cycle?",
                "Camden",
                "this week",
                evidence
            );

            Assert.Equal(AssistantMessageClass.Grounded, result.Class);
            Assert.Contains("12", result.Body, StringComparison.Ordinal);
            Assert.Contains("5", result.Body, StringComparison.Ordinal);
            Assert.Contains("3", result.Body, StringComparison.Ordinal);
            Assert.Contains("1–31 Oct 2026", result.Body, StringComparison.Ordinal);
            Assert.Contains("100", result.Body, StringComparison.Ordinal);
        }
    }
}

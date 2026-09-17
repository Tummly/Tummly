using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public class PendingSignupModelTests
    {
        [Fact]
        public void PendingSignupStatuses_AreStableStrings()
        {
            Assert.Equal("EmailPending", PendingSignupStatuses.EmailPending);
            Assert.Equal("Verified", PendingSignupStatuses.Verified);
            Assert.Equal(
                "OnboardingComplete",
                PendingSignupStatuses.OnboardingComplete
            );
            Assert.Equal(
                "AwaitingPayment",
                PendingSignupStatuses.AwaitingPayment
            );
            Assert.Equal("Provisioning", PendingSignupStatuses.Provisioning);
            Assert.Equal("Complete", PendingSignupStatuses.Complete);
            Assert.Equal("Abandoned", PendingSignupStatuses.Abandoned);
        }

        [Fact]
        public void RevolutOrderIntentPurposes_IncludesSignupPlan()
        {
            Assert.Equal(
                "signup_plan",
                RevolutOrderIntentPurposes.SignupPlan
            );
        }
    }
}

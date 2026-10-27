using TummlyBackend.Helpers;
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
        public void NewSecurityFields_DefaultToZeroAndNull()
        {
            var pending = new PendingSignup();

            Assert.Equal(0, pending.CodeGeneration);
            Assert.Equal(0, pending.CodesSentInWindow);
            Assert.Equal(0, pending.FailedVerifiesInWindow);
            Assert.Null(pending.SessionTokenIssuedAtUtc);
            Assert.Null(pending.AbuseWindowStartUtc);

            var otp = new OtpVerification();

            Assert.Equal(0, otp.FailedAttempts);
            Assert.Equal(0, otp.Generation);
            Assert.Null(otp.Purpose);
        }

        [Fact]
        public void OtpPurposes_AreStableStrings()
        {
            Assert.Equal("signup", OtpPurposes.Signup);
            Assert.Equal("sign_in", OtpPurposes.SignIn);
            Assert.Equal("trial", OtpPurposes.Trial);
            Assert.Equal("team_invite", OtpPurposes.TeamInvite);
        }
    }
}

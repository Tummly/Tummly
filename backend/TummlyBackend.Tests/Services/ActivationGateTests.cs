using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class ActivationGateTests
    {
        private readonly ActivationGate _gate = new();

        private static ActivationSubject NoState() =>
            new(null, null, null);

        private static ActivationSubject Pending() =>
            new(null, null, "hash");

        private static ActivationSubject WithinPeriod() =>
            new(
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(25),
                "hash"
            );

        private static ActivationSubject Expired() =>
            new(
                DateTime.UtcNow.AddDays(-40),
                DateTime.UtcNow.AddDays(-1),
                "hash"
            );

        [Fact]
        public void Decide_SignIn_NoState_AllowsAsPending()
        {
            var d = _gate.Decide(NoState(), ActivationIntent.SignIn);
            Assert.Equal(ActivationOutcome.Allow, d.Outcome);
            Assert.Equal(ActivationReason.Pending, d.Reason);
        }

        [Fact]
        public void Decide_SignIn_Pending_AllowsForRouting()
        {
            var d = _gate.Decide(Pending(), ActivationIntent.SignIn);
            Assert.Equal(ActivationOutcome.Allow, d.Outcome);
            Assert.Equal(ActivationReason.Pending, d.Reason);
        }

        [Fact]
        public void Decide_SignIn_WithinPeriod_Allows()
        {
            var d = _gate.Decide(WithinPeriod(), ActivationIntent.SignIn);
            Assert.Equal(ActivationOutcome.Allow, d.Outcome);
            Assert.Equal(ActivationReason.None, d.Reason);
        }

        [Fact]
        public void Decide_SignIn_Expired_Allows()
        {
            var d = _gate.Decide(Expired(), ActivationIntent.SignIn);
            Assert.Equal(ActivationOutcome.Allow, d.Outcome);
            Assert.Equal(ActivationReason.Expired, d.Reason);
        }

        [Fact]
        public void Decide_ApiAccess_NoState_BlocksAsPending()
        {
            var d = _gate.Decide(NoState(), ActivationIntent.ApiAccess);
            Assert.Equal(ActivationOutcome.Block, d.Outcome);
            Assert.Equal(ActivationReason.Pending, d.Reason);
            Assert.Equal(ActivationGate.ActivationRequiredMessage, d.Message);
        }

        [Fact]
        public void Decide_ApiAccess_Pending_Blocks()
        {
            var d = _gate.Decide(Pending(), ActivationIntent.ApiAccess);
            Assert.Equal(ActivationOutcome.Block, d.Outcome);
            Assert.Equal(ActivationReason.Pending, d.Reason);
            Assert.Equal(ActivationGate.ActivationRequiredMessage, d.Message);
        }

        [Fact]
        public void Decide_ApiAccess_WithinPeriod_Allows()
        {
            var d = _gate.Decide(WithinPeriod(), ActivationIntent.ApiAccess);
            Assert.Equal(ActivationOutcome.Allow, d.Outcome);
            Assert.Equal(ActivationReason.None, d.Reason);
        }

        [Fact]
        public void Decide_ApiAccess_Expired_Allows()
        {
            var d = _gate.Decide(Expired(), ActivationIntent.ApiAccess);
            Assert.Equal(ActivationOutcome.Allow, d.Outcome);
            Assert.Equal(ActivationReason.Expired, d.Reason);
        }

        [Fact]
        public void Decide_ActivatedWithoutExpires_BlocksAsExpired()
        {
            var subject = new ActivationSubject(
                DateTime.UtcNow.AddDays(-5),
                null,
                "hash"
            );

            var signIn = _gate.Decide(subject, ActivationIntent.SignIn);
            var api = _gate.Decide(subject, ActivationIntent.ApiAccess);

            Assert.Equal(ActivationOutcome.Block, signIn.Outcome);
            Assert.Equal(ActivationReason.Expired, signIn.Reason);
            Assert.Equal(ActivationGate.ActivationExpiredMessage, signIn.Message);

            Assert.Equal(ActivationOutcome.Block, api.Outcome);
            Assert.Equal(ActivationReason.Expired, api.Reason);
            Assert.Equal(ActivationGate.ActivationExpiredMessage, api.Message);
        }
    }
}

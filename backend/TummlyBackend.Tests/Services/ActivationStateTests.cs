using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class ActivationStateTests
    {
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
        public void HasActivationState_ReflectsHashOrActivatedAt()
        {
            Assert.False(ActivationState.HasActivationState(NoState()));
            Assert.True(ActivationState.HasActivationState(Pending()));
            Assert.True(ActivationState.HasActivationState(WithinPeriod()));
        }

        [Fact]
        public void RequiresActivation_WhenActivatedAtNull()
        {
            Assert.True(ActivationState.RequiresActivation(Pending()));
            Assert.False(ActivationState.RequiresActivation(WithinPeriod()));
            Assert.True(ActivationState.RequiresActivation(NoState()));
        }

        [Fact]
        public void IsPendingActivation_WhenHashSetAndNotActivated()
        {
            Assert.True(ActivationState.IsPendingActivation(Pending()));
            Assert.False(ActivationState.IsPendingActivation(WithinPeriod()));
            Assert.False(ActivationState.IsPendingActivation(NoState()));
        }

        [Fact]
        public void IsWithinActivationPeriod_WhenActivatedAndNotExpired()
        {
            Assert.True(ActivationState.IsWithinActivationPeriod(WithinPeriod()));
            Assert.False(ActivationState.IsWithinActivationPeriod(Expired()));
            Assert.False(ActivationState.IsWithinActivationPeriod(Pending()));
        }

        [Fact]
        public void IsActivationExpired_WhenActivatedAndPastExpiry()
        {
            Assert.True(ActivationState.IsActivationExpired(Expired()));
            Assert.False(ActivationState.IsActivationExpired(WithinPeriod()));
            Assert.False(ActivationState.IsActivationExpired(Pending()));
        }

        [Fact]
        public void IsActivationExpired_WhenActivatedWithoutExpires_TreatsAsExpired()
        {
            var subject = new ActivationSubject(
                DateTime.UtcNow.AddDays(-5),
                null,
                "hash"
            );

            Assert.True(ActivationState.IsActivationExpired(subject));
            Assert.False(ActivationState.IsWithinActivationPeriod(subject));
            Assert.False(ActivationState.RequiresActivation(subject));
        }

        [Fact]
        public void GetStatusDetail_ClassifiesEachState()
        {
            Assert.Null(ActivationState.GetStatusDetail(NoState()));
            Assert.Equal("pending", ActivationState.GetStatusDetail(Pending()));
            Assert.Equal("active", ActivationState.GetStatusDetail(WithinPeriod()));
            Assert.Equal("expired", ActivationState.GetStatusDetail(Expired()));
        }

        [Fact]
        public void GetStatusDetail_ActivatedWithoutExpires_ReturnsExpired()
        {
            var subject = new ActivationSubject(
                DateTime.UtcNow.AddDays(-5),
                null,
                "hash"
            );

            Assert.Equal("expired", ActivationState.GetStatusDetail(subject));
        }

        [Fact]
        public void GetStatusDetail_NullSubject_ReturnsNull()
        {
            Assert.Null(ActivationState.GetStatusDetail(null));
        }
    }
}

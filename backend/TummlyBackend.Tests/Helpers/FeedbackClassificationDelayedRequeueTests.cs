using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class FeedbackClassificationDelayedRequeueTests
    {
        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 10)]
        [InlineData(2, 20)]
        [InlineData(3, 40)]
        [InlineData(4, 60)]
        [InlineData(5, 60)]
        public void DelayBeforeReopen_UsesExponentialBackoffCappedAtOneHour(
            int delayedReopenCount,
            int expectedMinutes
        )
        {
            var delay = FeedbackClassificationDelayedRequeue.DelayBeforeReopen(
                delayedReopenCount,
                initialDelay: TimeSpan.FromMinutes(5),
                maxDelay: TimeSpan.FromHours(1)
            );

            Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), delay);
        }

        [Fact]
        public void CanReopen_AllowsWhenRetryableDueAndUnderCap()
        {
            var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

            Assert.True(
                FeedbackClassificationDelayedRequeue.CanReopen(
                    retryable: true,
                    retryAfter: now.AddMinutes(-1),
                    delayedReopenCount: 0,
                    maxDelayedReopens: 5,
                    now: now
                )
            );
        }

        [Fact]
        public void CanReopen_RejectsWhenNotDueYet()
        {
            var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

            Assert.False(
                FeedbackClassificationDelayedRequeue.CanReopen(
                    retryable: true,
                    retryAfter: now.AddMinutes(5),
                    delayedReopenCount: 0,
                    maxDelayedReopens: 5,
                    now: now
                )
            );
        }

        [Fact]
        public void CanReopen_RejectsWhenReopenCapReached()
        {
            var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

            Assert.False(
                FeedbackClassificationDelayedRequeue.CanReopen(
                    retryable: true,
                    retryAfter: now.AddMinutes(-1),
                    delayedReopenCount: 5,
                    maxDelayedReopens: 5,
                    now: now
                )
            );
        }
    }
}

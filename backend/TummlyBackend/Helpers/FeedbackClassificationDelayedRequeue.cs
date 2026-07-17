namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Delayed auto-requeue schedule for retryable Failed classification (ADR-0012).
    /// </summary>
    public static class FeedbackClassificationDelayedRequeue
    {
        public static TimeSpan DelayBeforeReopen(
            int delayedReopenCount,
            TimeSpan initialDelay,
            TimeSpan maxDelay
        )
        {
            if (delayedReopenCount < 0)
            {
                delayedReopenCount = 0;
            }

            // 5m → 10m → 20m → 40m → 60m (cap)
            var factor = 1 << Math.Min(delayedReopenCount, 30);
            var delay = TimeSpan.FromTicks(initialDelay.Ticks * factor);
            return delay > maxDelay ? maxDelay : delay;
        }

        public static bool CanReopen(
            bool retryable,
            DateTime? retryAfter,
            int delayedReopenCount,
            int maxDelayedReopens,
            DateTime now
        )
        {
            if (!retryable)
            {
                return false;
            }

            if (delayedReopenCount >= Math.Max(1, maxDelayedReopens))
            {
                return false;
            }

            if (retryAfter is null || retryAfter > now)
            {
                return false;
            }

            return true;
        }
    }
}

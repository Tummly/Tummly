using System.Globalization;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Freshness check for the <c>Revolut-Request-Timestamp</c> header.
    /// Values above 10^11 are read as milliseconds, otherwise as seconds.
    /// </summary>
    public static class RevolutWebhookTimestamp
    {
        private const long MillisecondsThreshold = 100_000_000_000L;

        public static bool IsFresh(
            string? timestamp,
            DateTimeOffset nowUtc,
            TimeSpan tolerance
        )
        {
            if (
                string.IsNullOrWhiteSpace(timestamp)
                || !long.TryParse(
                    timestamp.Trim(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var value
                )
            )
            {
                return false;
            }

            DateTimeOffset sentAt;
            try
            {
                sentAt =
                    value > MillisecondsThreshold
                        ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                        : DateTimeOffset.FromUnixTimeSeconds(value);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            return (nowUtc - sentAt).Duration() <= tolerance;
        }
    }
}

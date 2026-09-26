using Microsoft.Extensions.Caching.Memory;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Caps new Guest Form thank-you Offer issues (re-shows do not consume).
    /// Per QR token: 5 / hour. Per client IP: 10 / hour.
    /// </summary>
    public static class ThankYouIssueAbuseCaps
    {
        public const int MaxNewIssuesPerTokenPerHour = 5;
        public const int MaxNewIssuesPerIpPerHour = 10;

        public static bool IsUnderCap(
            IMemoryCache cache,
            string token,
            string? clientIp
        )
        {
            if (Count(cache, TokenKey(token)) >= MaxNewIssuesPerTokenPerHour)
            {
                return false;
            }

            if (
                !string.IsNullOrWhiteSpace(clientIp)
                && Count(cache, IpKey(clientIp)) >= MaxNewIssuesPerIpPerHour
            )
            {
                return false;
            }

            return true;
        }

        public static void RecordNewIssue(
            IMemoryCache cache,
            string token,
            string? clientIp
        )
        {
            Record(cache, TokenKey(token));

            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                Record(cache, IpKey(clientIp));
            }
        }

        private static string TokenKey(string token)
            => $"thankyou_issue_token:{token}";

        private static string IpKey(string clientIp)
            => $"thankyou_issue_ip:{clientIp}";

        private static int Count(IMemoryCache cache, string key)
        {
            var stamps = cache.GetOrCreate(
                key,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    return new List<DateTime>();
                }
            );

            return stamps!.Count;
        }

        private static void Record(IMemoryCache cache, string key)
        {
            var stamps = cache.GetOrCreate(
                key,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    return new List<DateTime>();
                }
            );

            stamps!.Add(DateTime.UtcNow);
        }
    }
}

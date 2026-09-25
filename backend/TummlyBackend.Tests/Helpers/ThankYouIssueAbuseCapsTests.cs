using Microsoft.Extensions.Caching.Memory;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class ThankYouIssueAbuseCapsTests
    {
        [Fact]
        public void IsUnderCap_AllowsUntilTokenLimitThenBlocks()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            const string token = "tok-a";
            const string ip = "203.0.113.10";

            for (var i = 0; i < ThankYouIssueAbuseCaps.MaxNewIssuesPerTokenPerHour; i++)
            {
                Assert.True(ThankYouIssueAbuseCaps.IsUnderCap(cache, token, ip));
                ThankYouIssueAbuseCaps.RecordNewIssue(cache, token, ip);
            }

            Assert.False(ThankYouIssueAbuseCaps.IsUnderCap(cache, token, ip));
            Assert.True(
                ThankYouIssueAbuseCaps.IsUnderCap(cache, "tok-b", ip)
            );
        }

        [Fact]
        public void IsUnderCap_BlocksWhenIpLimitReachedAcrossTokens()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            const string ip = "198.51.100.7";

            for (var i = 0; i < ThankYouIssueAbuseCaps.MaxNewIssuesPerIpPerHour; i++)
            {
                var token = $"tok-{i}";
                Assert.True(ThankYouIssueAbuseCaps.IsUnderCap(cache, token, ip));
                ThankYouIssueAbuseCaps.RecordNewIssue(cache, token, ip);
            }

            Assert.False(
                ThankYouIssueAbuseCaps.IsUnderCap(cache, "tok-extra", ip)
            );
            Assert.True(
                ThankYouIssueAbuseCaps.IsUnderCap(cache, "tok-extra", "198.51.100.8")
            );
        }

        [Fact]
        public void IsUnderCap_IgnoresNullIp()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            const string token = "tok-null-ip";

            for (var i = 0; i < ThankYouIssueAbuseCaps.MaxNewIssuesPerTokenPerHour; i++)
            {
                Assert.True(ThankYouIssueAbuseCaps.IsUnderCap(cache, token, null));
                ThankYouIssueAbuseCaps.RecordNewIssue(cache, token, null);
            }

            Assert.False(ThankYouIssueAbuseCaps.IsUnderCap(cache, token, null));
        }
    }
}

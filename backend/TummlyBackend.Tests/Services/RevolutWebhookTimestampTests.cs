using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class RevolutWebhookTimestampTests
    {
        private static readonly DateTimeOffset Now = new(
            2026,
            10,
            26,
            12,
            0,
            0,
            TimeSpan.Zero
        );

        public static IEnumerable<object?[]> FreshnessCases()
        {
            yield return [Now.ToUnixTimeSeconds().ToString(), true];
            yield return [Now.AddMinutes(-4).ToUnixTimeMilliseconds().ToString(), true];
            yield return [Now.AddMinutes(-4).ToUnixTimeSeconds().ToString(), true];
            yield return [Now.AddMinutes(4).ToUnixTimeSeconds().ToString(), true];
            yield return [Now.AddMinutes(-6).ToUnixTimeSeconds().ToString(), false];
            yield return [Now.AddMinutes(6).ToUnixTimeSeconds().ToString(), false];
            yield return ["", false];
            yield return ["abc", false];
            yield return [null, false];
        }

        [Theory]
        [MemberData(nameof(FreshnessCases))]
        public void IsFresh_UsesFiveMinuteWindow(string? timestamp, bool expected)
        {
            var fresh = RevolutWebhookTimestamp.IsFresh(
                timestamp,
                Now,
                TimeSpan.FromMinutes(5)
            );

            Assert.Equal(expected, fresh);
        }
    }
}

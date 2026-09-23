using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="ThankYouOfferUnlockToken"/> — signed unlock handoff.
    /// </summary>
    public class ThankYouOfferUnlockTokenTests
    {
        private const string Secret = "test-unlock-secret-value";

        [Fact]
        public void Create_ThenTryVerify_RoundTripsPayload()
        {
            var issued = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
            var token = ThankYouOfferUnlockToken.Create(
                feedbackId: 41,
                locationGuestId: 7,
                locationId: 3,
                channel: "sms",
                issued,
                Secret
            );

            Assert.True(
                ThankYouOfferUnlockToken.TryVerify(
                    token,
                    Secret,
                    out var feedbackId,
                    out var locationGuestId,
                    out var locationId,
                    out var channel,
                    out var error
                )
            );
            Assert.Null(error);
            Assert.Equal(41, feedbackId);
            Assert.Equal(7, locationGuestId);
            Assert.Equal(3, locationId);
            Assert.Equal("sms", channel);
        }

        [Fact]
        public void TryVerify_RejectsTamperedToken()
        {
            var token = ThankYouOfferUnlockToken.Create(
                feedbackId: 1,
                locationGuestId: 2,
                locationId: 3,
                channel: "email",
                DateTime.UtcNow,
                Secret
            );
            var tampered = token[..^1] + (token[^1] == 'a' ? 'b' : 'a');

            Assert.False(
                ThankYouOfferUnlockToken.TryVerify(
                    tampered,
                    Secret,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var error
                )
            );
            Assert.Equal("signature", error);
        }
    }
}

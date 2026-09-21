using Microsoft.Extensions.Configuration;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="UnsubscribeLink"/> — signed one-click and restaurant form URLs.
    /// </summary>
    public class UnsubscribeLinkTests
    {
        private const string Secret =
            "integration-test-jwt-secret-32chars!";

        [Fact]
        public void ForLocationGuest_BuildsUrlWithTokenQuery()
        {
            var issuedAt = new DateTime(
                2026,
                9,
                1,
                12,
                0,
                0,
                DateTimeKind.Utc
            );

            var href = UnsubscribeLink.ForLocationGuest(
                frontendBaseUrl: "https://app.tummly.test/",
                locationGuestId: 42,
                restaurantId: 7,
                secret: Secret,
                issuedAtUtc: issuedAt
            );

            Assert.StartsWith(
                "https://app.tummly.test/unsubscribe?t=",
                href
            );

            var token = href["https://app.tummly.test/unsubscribe?t=".Length..];
            Assert.True(
                UnsubscribeToken.TryVerify(
                    token,
                    Secret,
                    out var locationGuestId,
                    out var restaurantId,
                    out var error
                )
            );
            Assert.Null(error);
            Assert.Equal(42, locationGuestId);
            Assert.Equal(7, restaurantId);
        }

        [Fact]
        public void ForRestaurant_BuildsUrlWithRestaurantIdQuery()
        {
            var href = UnsubscribeLink.ForRestaurant(
                frontendBaseUrl: "https://app.tummly.test/",
                restaurantId: 7
            );

            Assert.Equal(
                "https://app.tummly.test/unsubscribe?restaurantId=7",
                href
            );
        }

        [Fact]
        public void PreferSignedOrRestaurant_UsesToken_WhenSecretPresent()
        {
            var href = UnsubscribeLink.PreferSignedOrRestaurant(
                frontendBaseUrl: "https://app.tummly.test",
                restaurantId: 7,
                locationGuestId: 42,
                secret: Secret
            );

            Assert.NotNull(href);
            Assert.StartsWith(
                "https://app.tummly.test/unsubscribe?t=",
                href
            );
        }

        [Fact]
        public void PreferSignedOrRestaurant_UsesRestaurantId_WhenSecretMissing()
        {
            var href = UnsubscribeLink.PreferSignedOrRestaurant(
                frontendBaseUrl: "https://app.tummly.test",
                restaurantId: 7,
                locationGuestId: 42,
                secret: null
            );

            Assert.Equal(
                "https://app.tummly.test/unsubscribe?restaurantId=7",
                href
            );
        }

        [Fact]
        public void PreferSignedOrRestaurant_UsesRestaurantId_WhenLocationGuestMissing()
        {
            var href = UnsubscribeLink.PreferSignedOrRestaurant(
                frontendBaseUrl: "https://app.tummly.test",
                restaurantId: 7,
                locationGuestId: null,
                secret: Secret
            );

            Assert.Equal(
                "https://app.tummly.test/unsubscribe?restaurantId=7",
                href
            );
        }

        [Fact]
        public void PreferSignedOrRestaurant_ReturnsNull_WhenRestaurantUnknown()
        {
            var href = UnsubscribeLink.PreferSignedOrRestaurant(
                frontendBaseUrl: "https://app.tummly.test",
                restaurantId: 0,
                locationGuestId: 42,
                secret: null
            );

            Assert.Null(href);
        }

        [Fact]
        public void ResolveSigningSecret_PrefersUnsubscribeSecret()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Unsubscribe:SigningSecret"] = "unsubscribe-secret",
                        ["JwtSettings:Secret"] = "jwt-secret",
                    }
                )
                .Build();

            Assert.Equal(
                "unsubscribe-secret",
                UnsubscribeLink.ResolveSigningSecret(config)
            );
        }

        [Fact]
        public void ResolveSigningSecret_FallsBackToJwtSecret()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["JwtSettings:Secret"] = "jwt-secret",
                    }
                )
                .Build();

            Assert.Equal(
                "jwt-secret",
                UnsubscribeLink.ResolveSigningSecret(config)
            );
        }
    }
}

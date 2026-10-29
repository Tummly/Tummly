using Microsoft.Extensions.Configuration;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="UnsubscribeLink"/> — preference centre links and RFC 8058 one-click URLs.
    /// </summary>
    public class UnsubscribeLinkTests
    {
        private const string Secret =
            "integration-test-jwt-secret-32chars!";

        [Fact]
        public void ForLocationGuest_UsesPreferencesPath()
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
                "https://app.tummly.test/preferences?t=",
                href
            );

            var token = href["https://app.tummly.test/preferences?t=".Length..];
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
        public void ForRestaurant_UsesPreferencesPath()
        {
            var href = UnsubscribeLink.ForRestaurant(
                frontendBaseUrl: "https://app.tummly.test/"
            );

            Assert.Equal("https://app.tummly.test/preferences", href);
        }

        [Fact]
        public void OneClickUrl_BuildsApiOneClickPathWithToken()
        {
            var href = UnsubscribeLink.OneClickUrl(
                apiBaseUrl: "https://api.tummly.test/",
                token: "payload.sig"
            );

            Assert.Equal(
                "https://api.tummly.test/api/public/preferences/one-click?t=payload.sig",
                href
            );
        }

        [Fact]
        public void OneClickUrlFor_ReturnsNull_WhenApiBaseUnset()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["JwtSettings:Secret"] = Secret,
                    }
                )
                .Build();

            Assert.Null(
                UnsubscribeLink.OneClickUrlFor(config, restaurantId: 7, locationGuestId: 42)
            );
        }

        [Fact]
        public void OneClickUrlFor_ReturnsVerifiableUrl_WhenApiBaseSet()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["JwtSettings:Secret"] = Secret,
                        ["Api:PublicBaseUrl"] = "https://api.tummly.test",
                    }
                )
                .Build();

            var href = UnsubscribeLink.OneClickUrlFor(config, restaurantId: 7, locationGuestId: 42);

            Assert.NotNull(href);
            var prefix = "https://api.tummly.test/api/public/preferences/one-click?t=";
            Assert.StartsWith(prefix, href);
            Assert.True(
                UnsubscribeToken.TryVerify(
                    href[prefix.Length..],
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
        public void OneClickUrlFor_ReturnsNull_WhenApiBaseIsNotHttps()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["JwtSettings:Secret"] = Secret,
                        ["Api:PublicBaseUrl"] = "http://api.tummly.test",
                    }
                )
                .Build();

            Assert.Null(
                UnsubscribeLink.OneClickUrlFor(config, restaurantId: 7, locationGuestId: 42)
            );
        }

        [Fact]
        public void OneClickUrlFor_AcceptsUppercaseHttpsScheme()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["JwtSettings:Secret"] = Secret,
                        ["Api:PublicBaseUrl"] = "HTTPS://api.tummly.test",
                    }
                )
                .Build();

            Assert.NotNull(
                UnsubscribeLink.OneClickUrlFor(config, restaurantId: 7, locationGuestId: 42)
            );
        }

        [Fact]
        public void OneClickUrlFor_ReturnsNull_WhenLocationGuestMissing()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["JwtSettings:Secret"] = Secret,
                        ["Api:PublicBaseUrl"] = "https://api.tummly.test",
                    }
                )
                .Build();

            Assert.Null(
                UnsubscribeLink.OneClickUrlFor(config, restaurantId: 7, locationGuestId: null)
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
                "https://app.tummly.test/preferences?t=",
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
                "https://app.tummly.test/preferences",
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
                "https://app.tummly.test/preferences",
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

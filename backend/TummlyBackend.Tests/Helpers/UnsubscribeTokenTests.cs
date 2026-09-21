using System.Text;
using System.Text.Json;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="UnsubscribeToken"/> — HMAC URL token for one-click email unsubscribe.
    /// </summary>
    public class UnsubscribeTokenTests
    {
        private const string Secret =
            "integration-test-jwt-secret-32chars!";

        [Fact]
        public void Create_ThenTryVerify_ReturnsLocationGuestAndRestaurant()
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

            var token = UnsubscribeToken.Create(
                locationGuestId: 42,
                restaurantId: 7,
                issuedAtUtc: issuedAt,
                secret: Secret
            );

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
            Assert.DoesNotContain("+", token);
            Assert.DoesNotContain("/", token);
            Assert.DoesNotContain("=", token);
            Assert.Contains(".", token);
        }

        [Fact]
        public void TryVerify_Fails_WhenPayloadTampered()
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

            var token = UnsubscribeToken.Create(
                locationGuestId: 42,
                restaurantId: 7,
                issuedAtUtc: issuedAt,
                secret: Secret
            );

            var parts = token.Split('.');
            Assert.Equal(2, parts.Length);

            var payloadJson = Encoding.UTF8.GetString(
                Base64UrlDecode(parts[0])
            );
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            var tampered = JsonSerializer.Serialize(
                new
                {
                    lg = 99,
                    r = root.GetProperty("r").GetInt32(),
                    iat = root.GetProperty("iat").GetInt64(),
                    exp = root.GetProperty("exp").GetInt64(),
                }
            );
            var tamperedToken =
                Base64UrlEncode(Encoding.UTF8.GetBytes(tampered))
                + "."
                + parts[1];

            Assert.False(
                UnsubscribeToken.TryVerify(
                    tamperedToken,
                    Secret,
                    out _,
                    out _,
                    out var error
                )
            );
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void TryVerify_Fails_WhenExpired()
        {
            var issuedAt = new DateTime(
                2026,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc
            );

            var token = UnsubscribeToken.Create(
                locationGuestId: 42,
                restaurantId: 7,
                issuedAtUtc: issuedAt,
                secret: Secret,
                ttl: TimeSpan.FromHours(1)
            );

            Assert.False(
                UnsubscribeToken.TryVerify(
                    token,
                    Secret,
                    out _,
                    out _,
                    out var error
                )
            );
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void Create_UsesNinetyDayDefaultTtl()
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

            var token = UnsubscribeToken.Create(
                locationGuestId: 1,
                restaurantId: 2,
                issuedAtUtc: issuedAt,
                secret: Secret
            );

            var payloadJson = Encoding.UTF8.GetString(
                Base64UrlDecode(token.Split('.')[0])
            );
            using var doc = JsonDocument.Parse(payloadJson);
            var iat = doc.RootElement.GetProperty("iat").GetInt64();
            var exp = doc.RootElement.GetProperty("exp").GetInt64();

            Assert.Equal(
                ((DateTimeOffset)issuedAt).ToUnixTimeSeconds(),
                iat
            );
            Assert.Equal(
                iat + (UnsubscribeToken.DefaultTtlHours * 3600L),
                exp
            );
            Assert.Equal(90 * 24, UnsubscribeToken.DefaultTtlHours);
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert
                .ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var padded = input.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }

            return Convert.FromBase64String(padded);
        }
    }
}

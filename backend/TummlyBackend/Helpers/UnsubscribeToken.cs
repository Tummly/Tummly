using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// URL-safe HMAC token for one-click email unsubscribe links.
    /// Format: <c>base64url(payload).base64url(hmacsha256)</c>.
    /// </summary>
    public static class UnsubscribeToken
    {
        public const int DefaultTtlHours = 90 * 24;

        private static readonly JsonSerializerOptions PayloadJsonOptions =
            new()
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            };

        public static string Create(
            int locationGuestId,
            int restaurantId,
            DateTime issuedAtUtc,
            string secret,
            TimeSpan? ttl = null
        )
        {
            if (string.IsNullOrEmpty(secret))
            {
                throw new ArgumentException(
                    "Secret is required.",
                    nameof(secret)
                );
            }

            var issued =
                issuedAtUtc.Kind == DateTimeKind.Utc
                    ? issuedAtUtc
                    : issuedAtUtc.ToUniversalTime();
            var lifetime = ttl ?? TimeSpan.FromHours(DefaultTtlHours);
            var iat = new DateTimeOffset(issued).ToUnixTimeSeconds();
            var exp = new DateTimeOffset(issued.Add(lifetime))
                .ToUnixTimeSeconds();

            var payload = new Payload
            {
                Lg = locationGuestId,
                R = restaurantId,
                Iat = iat,
                Exp = exp,
            };

            var payloadBytes = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(payload, PayloadJsonOptions)
            );
            var payloadPart = Base64UrlEncode(payloadBytes);
            var signaturePart = Base64UrlEncode(
                ComputeHmac(payloadBytes, secret)
            );

            return payloadPart + "." + signaturePart;
        }

        public static bool TryVerify(
            string token,
            string secret,
            out int locationGuestId,
            out int restaurantId,
            out string? error
        )
        {
            locationGuestId = 0;
            restaurantId = 0;
            error = null;

            if (string.IsNullOrWhiteSpace(token))
            {
                error = "missing";
                return false;
            }

            if (string.IsNullOrEmpty(secret))
            {
                error = "secret";
                return false;
            }

            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                error = "format";
                return false;
            }

            byte[] payloadBytes;
            byte[] providedSignature;
            try
            {
                payloadBytes = Base64UrlDecode(parts[0]);
                providedSignature = Base64UrlDecode(parts[1]);
            }
            catch (FormatException)
            {
                error = "encoding";
                return false;
            }

            var expectedSignature = ComputeHmac(payloadBytes, secret);
            if (
                expectedSignature.Length != providedSignature.Length
                || !CryptographicOperations.FixedTimeEquals(
                    expectedSignature,
                    providedSignature
                )
            )
            {
                error = "signature";
                return false;
            }

            Payload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<Payload>(
                    payloadBytes,
                    PayloadJsonOptions
                );
            }
            catch (JsonException)
            {
                error = "payload";
                return false;
            }

            if (payload == null)
            {
                error = "payload";
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (payload.Exp < now)
            {
                error = "expired";
                return false;
            }

            locationGuestId = payload.Lg;
            restaurantId = payload.R;
            return true;
        }

        private static byte[] ComputeHmac(byte[] payloadBytes, string secret)
        {
            var key = Encoding.UTF8.GetBytes(secret);
            return HMACSHA256.HashData(key, payloadBytes);
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

        private sealed class Payload
        {
            [JsonPropertyName("lg")]
            public int Lg { get; set; }

            [JsonPropertyName("r")]
            public int R { get; set; }

            [JsonPropertyName("iat")]
            public long Iat { get; set; }

            [JsonPropertyName("exp")]
            public long Exp { get; set; }
        }
    }
}

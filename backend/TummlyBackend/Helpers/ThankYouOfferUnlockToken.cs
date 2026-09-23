using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Short-lived HMAC handoff for Guest Form thank-you unlock CTA.
    /// Format: <c>base64url(payload).base64url(hmacsha256)</c>.
    /// </summary>
    public static class ThankYouOfferUnlockToken
    {
        public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(2);

        private static readonly JsonSerializerOptions PayloadJsonOptions =
            new()
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            };

        public static string Create(
            int feedbackId,
            int locationGuestId,
            int locationId,
            string channel,
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

            if (channel is not ("email" or "sms"))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(channel),
                    channel,
                    "Channel must be email or sms."
                );
            }

            var issued =
                issuedAtUtc.Kind == DateTimeKind.Utc
                    ? issuedAtUtc
                    : issuedAtUtc.ToUniversalTime();
            var lifetime = ttl ?? DefaultTtl;
            var iat = new DateTimeOffset(issued).ToUnixTimeSeconds();
            var exp = new DateTimeOffset(issued.Add(lifetime))
                .ToUnixTimeSeconds();

            var payload = new Payload
            {
                F = feedbackId,
                Lg = locationGuestId,
                L = locationId,
                C = channel,
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
            out int feedbackId,
            out int locationGuestId,
            out int locationId,
            out string channel,
            out string? error
        )
        {
            feedbackId = 0;
            locationGuestId = 0;
            locationId = 0;
            channel = string.Empty;
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

            if (payload.C is not ("email" or "sms"))
            {
                error = "channel";
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (payload.Exp < now)
            {
                error = "expired";
                return false;
            }

            feedbackId = payload.F;
            locationGuestId = payload.Lg;
            locationId = payload.L;
            channel = payload.C;
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
            [JsonPropertyName("f")]
            public int F { get; set; }

            [JsonPropertyName("lg")]
            public int Lg { get; set; }

            [JsonPropertyName("l")]
            public int L { get; set; }

            [JsonPropertyName("c")]
            public string C { get; set; } = string.Empty;

            [JsonPropertyName("iat")]
            public long Iat { get; set; }

            [JsonPropertyName("exp")]
            public long Exp { get; set; }
        }
    }
}

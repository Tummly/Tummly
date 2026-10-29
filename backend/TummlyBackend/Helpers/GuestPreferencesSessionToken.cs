using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// HMAC session token for the guest preference centre, sent in the
    /// <see cref="HeaderName"/> request header.
    /// Format: <c>base64url(payload).base64url(hmacsha256)</c>.
    /// The payload carries <c>"typ":"gps"</c> so an unsubscribe token signed
    /// with the same secret is never accepted as a session.
    /// </summary>
    public static class GuestPreferencesSessionToken
    {
        public const string HeaderName = "X-Guest-Preferences-Session";

        public const string PayloadType = "gps";

        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

        private static readonly JsonSerializerOptions PayloadJsonOptions =
            new()
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            };

        public static string Create(
            int tummlyGuestId,
            int contactId,
            DateTime issuedAtUtc,
            string secret
        )
        {
            if (string.IsNullOrEmpty(secret))
            {
                throw new ArgumentException(
                    "Secret is required.",
                    nameof(secret)
                );
            }

            var issued = ToUtc(issuedAtUtc);
            var exp = new DateTimeOffset(issued.Add(Lifetime))
                .ToUnixTimeSeconds();

            var payload = new Payload
            {
                Typ = PayloadType,
                Tg = tummlyGuestId,
                C = contactId,
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
            string? token,
            string secret,
            DateTime nowUtc,
            out int tummlyGuestId,
            out int contactId
        )
        {
            tummlyGuestId = 0;
            contactId = 0;

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(secret))
            {
                return false;
            }

            var parts = token.Split('.');
            if (parts.Length != 2)
            {
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
                return false;
            }

            if (payload == null || payload.Typ != PayloadType)
            {
                return false;
            }

            var now = new DateTimeOffset(ToUtc(nowUtc)).ToUnixTimeSeconds();
            if (payload.Exp < now)
            {
                return false;
            }

            tummlyGuestId = payload.Tg;
            contactId = payload.C;
            return true;
        }

        /// <summary>
        /// <c>GuestPreferences:SessionSigningSecret</c> when set; otherwise the
        /// unsubscribe signing secret.
        /// </summary>
        public static string? ResolveSecret(IConfiguration configuration)
        {
            var dedicated = configuration["GuestPreferences:SessionSigningSecret"];
            if (!string.IsNullOrWhiteSpace(dedicated))
            {
                return dedicated.Trim();
            }

            return UnsubscribeLink.ResolveSigningSecret(configuration);
        }

        /// <summary>
        /// True when session tokens would sign with the shared unsubscribe or JWT secret: the
        /// environment is Production and <c>GuestPreferences:SessionSigningSecret</c> is blank.
        /// Startup logs a warning for this. It does not fail.
        /// </summary>
        public static bool FallsBackToSharedSecret(bool isProduction, IConfiguration configuration) =>
            isProduction
            && string.IsNullOrWhiteSpace(configuration["GuestPreferences:SessionSigningSecret"]);

        private static DateTime ToUtc(DateTime value) =>
            value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

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
            [JsonPropertyName("typ")]
            public string? Typ { get; set; }

            [JsonPropertyName("tg")]
            public int Tg { get; set; }

            [JsonPropertyName("c")]
            public int C { get; set; }

            [JsonPropertyName("exp")]
            public long Exp { get; set; }
        }
    }
}

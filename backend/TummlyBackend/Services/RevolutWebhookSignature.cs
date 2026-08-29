using System.Security.Cryptography;
using System.Text;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Revolut Merchant webhook HMAC verify:
    /// payload <c>v1.{timestamp}.{raw body}</c>, header
    /// <c>Revolut-Signature</c> as <c>v1=&lt;hex&gt;</c> (possibly multiple).
    /// </summary>
    public static class RevolutWebhookSignature
    {
        public static bool Verify(
            string signingSecret,
            string timestamp,
            string rawBody,
            string signatureHeader
        )
        {
            if (
                string.IsNullOrWhiteSpace(signingSecret)
                || string.IsNullOrWhiteSpace(timestamp)
                || signatureHeader is null
            )
            {
                return false;
            }

            var payload = $"v1.{timestamp.Trim()}.{rawBody}";
            var expectedHex = ComputeHex(signingSecret.Trim(), payload);
            if (expectedHex.Length == 0)
            {
                return false;
            }

            foreach (
                var part in signatureHeader.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries
                )
            )
            {
                if (
                    !part.StartsWith("v1=", StringComparison.Ordinal)
                    || part.Length <= 3
                )
                {
                    continue;
                }

                var provided = part[3..];
                if (FixedTimeEqualsHex(expectedHex, provided))
                {
                    return true;
                }
            }

            return false;
        }

        public static string SignForTests(
            string signingSecret,
            string timestamp,
            string rawBody
        )
        {
            var payload = $"v1.{timestamp}.{rawBody}";
            return "v1=" + ComputeHex(signingSecret, payload);
        }

        private static string ComputeHex(string secret, string payload)
        {
            var key = Encoding.UTF8.GetBytes(secret);
            var data = Encoding.UTF8.GetBytes(payload);
            var hash = HMACSHA256.HashData(key, data);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool FixedTimeEqualsHex(string expected, string provided)
        {
            if (expected.Length != provided.Length)
            {
                return false;
            }

            try
            {
                var a = Convert.FromHexString(expected);
                var b = Convert.FromHexString(
                    provided.ToLowerInvariant()
                );
                return CryptographicOperations.FixedTimeEquals(a, b);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}

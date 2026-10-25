using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// HMAC-SHA256 (lower-case hex) of a contact key using
    /// <c>GuestSuppression:HashKey</c>.
    /// </summary>
    public sealed class GuestContactHasher : IGuestContactHasher
    {
        public const string ConfigurationKey = "GuestSuppression:HashKey";

        public const string TestingFallbackKey =
            "tests-only-guest-suppression-key";

        private readonly byte[] _keyBytes;

        public GuestContactHasher(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    "Hash key is required.",
                    nameof(key)
                );
            }

            _keyBytes = Encoding.UTF8.GetBytes(key);
        }

        public string Hash(string contactKey)
        {
            ArgumentNullException.ThrowIfNull(contactKey);

            var data = Encoding.UTF8.GetBytes(contactKey);
            var hash = HMACSHA256.HashData(_keyBytes, data);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static GuestContactHasher FromConfiguration(
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            var key = configuration[ConfigurationKey];
            if (!string.IsNullOrWhiteSpace(key))
            {
                return new GuestContactHasher(key.Trim());
            }

            if (
                environment.IsDevelopment()
                || environment.IsEnvironment("Testing")
            )
            {
                return new GuestContactHasher(TestingFallbackKey);
            }

            throw new InvalidOperationException(
                "GuestSuppression:HashKey is required."
            );
        }
    }
}

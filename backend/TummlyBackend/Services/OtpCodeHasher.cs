using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// HMAC-SHA256 (lower-case hex) of one-time codes and IP addresses using
    /// <c>Otp:HashKey</c>.
    /// </summary>
    public sealed class OtpCodeHasher : IOtpCodeHasher
    {
        public const string ConfigurationKey = "Otp:HashKey";

        public const string TestingFallbackKey = "tests-only-otp-hash-key";

        private readonly byte[] _keyBytes;

        public OtpCodeHasher(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Hash key is required.", nameof(key));
            }

            _keyBytes = Encoding.UTF8.GetBytes(key);
        }

        public string HashCode(string purpose, string subjectId, int generation, string code)
        {
            ArgumentNullException.ThrowIfNull(purpose);
            ArgumentNullException.ThrowIfNull(subjectId);
            ArgumentNullException.ThrowIfNull(code);

            return Hmac($"{purpose}|{subjectId}|{generation}|{code}");
        }

        public string HashIp(string ip)
        {
            ArgumentNullException.ThrowIfNull(ip);

            return Hmac($"ip|{ip}");
        }

        private string Hmac(string input)
        {
            var data = Encoding.UTF8.GetBytes(input);
            var hash = HMACSHA256.HashData(_keyBytes, data);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static OtpCodeHasher FromConfiguration(
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            var key = configuration[ConfigurationKey];
            if (!string.IsNullOrWhiteSpace(key))
            {
                return new OtpCodeHasher(key.Trim());
            }

            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                return new OtpCodeHasher(TestingFallbackKey);
            }

            throw new InvalidOperationException("Otp:HashKey is required.");
        }
    }
}

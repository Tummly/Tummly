using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="GuestContactHasher"/> — HMAC-SHA256 hex of contact keys.
    /// </summary>
    public class GuestContactHasherTests
    {
        [Fact]
        public void Hash_SameInput_Same64CharLowerHex()
        {
            var hasher = new GuestContactHasher("test-key-a");
            var a = hasher.Hash("email:alex@example.com");
            var b = hasher.Hash("email:alex@example.com");

            Assert.Equal(a, b);
            Assert.Equal(64, a.Length);
            Assert.Matches("^[0-9a-f]{64}$", a);
        }

        [Fact]
        public void Hash_DifferentKey_DifferentHash()
        {
            var left = new GuestContactHasher("key-one").Hash(
                "email:alex@example.com"
            );
            var right = new GuestContactHasher("key-two").Hash(
                "email:alex@example.com"
            );

            Assert.NotEqual(left, right);
        }

        [Fact]
        public void FromConfiguration_ThrowsInProduction_WhenKeyMissing()
        {
            var config = new ConfigurationBuilder().Build();
            var env = new TestHostEnvironment("Production");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                GuestContactHasher.FromConfiguration(config, env)
            );
            Assert.Equal(
                "GuestSuppression:HashKey is required.",
                ex.Message
            );
        }

        [Fact]
        public void FromConfiguration_UsesFixedKeyInTesting_WhenMissing()
        {
            var config = new ConfigurationBuilder().Build();
            var env = new TestHostEnvironment("Testing");

            var hasher = GuestContactHasher.FromConfiguration(config, env);
            var expected = new GuestContactHasher(
                "tests-only-guest-suppression-key"
            ).Hash("phone:+447123456789");

            Assert.Equal(expected, hasher.Hash("phone:+447123456789"));
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; }
                = AppContext.BaseDirectory;

            public IFileProvider ContentRootFileProvider { get; set; }
                = new NullFileProvider();
        }
    }
}

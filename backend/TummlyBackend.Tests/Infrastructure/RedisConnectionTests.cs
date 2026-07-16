using Microsoft.Extensions.Configuration;
using TummlyBackend.Infrastructure;

namespace TummlyBackend.Tests.Infrastructure
{
    public sealed class RedisConnectionTests
    {
        [Fact]
        public void TryResolve_ReturnsNull_WhenConnectionStringMissing()
        {
            var configuration = new ConfigurationBuilder().Build();

            Assert.Null(RedisConnection.TryResolve(configuration));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void TryResolve_ReturnsNull_WhenConnectionStringBlank(
            string value
        )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Redis"] = value,
                    }
                )
                .Build();

            Assert.Null(RedisConnection.TryResolve(configuration));
        }

        [Fact]
        public void TryResolve_ReturnsTrimmedConnectionString_WhenSet()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Redis"] = "  localhost:6379  ",
                    }
                )
                .Build();

            var resolved = RedisConnection.TryResolve(configuration);

            Assert.NotNull(resolved);
            Assert.Equal("localhost:6379", resolved);
        }
    }
}

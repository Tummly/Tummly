using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Hubs;

namespace TummlyBackend.Tests.Hubs
{
    public sealed class OperatorSignalRRegistrationTests
    {
        [Fact]
        public void AddOperatorSignalR_WithoutRedis_DoesNotInvokeBackplane()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();
            var invoked = false;

            services.AddOperatorSignalR(
                configuration,
                (_, _) => invoked = true
            );

            Assert.False(invoked);
        }

        [Fact]
        public void AddOperatorSignalR_WithRedis_InvokesBackplaneWithResolvedString()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Redis"] = "  localhost:6379  ",
                    }
                )
                .Build();
            string? seen = null;

            services.AddOperatorSignalR(
                configuration,
                (_, connectionString) => seen = connectionString
            );

            Assert.Equal("localhost:6379", seen);
        }
    }
}

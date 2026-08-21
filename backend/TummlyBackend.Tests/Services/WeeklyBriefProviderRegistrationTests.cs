using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// CI must register Fake Weekly brief provider when Azure is stubbed
    /// (same FeedbackClassification Fake switch as Home recommendation).
    /// </summary>
    public class WeeklyBriefProviderRegistrationTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public WeeklyBriefProviderRegistrationTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
        }

        [Fact]
        public void FakeWeeklyBriefProvider_IsRegistered_WhenAzureStubbed()
        {
            using var scope = _factory.Services.CreateScope();
            var provider = scope.ServiceProvider
                .GetRequiredService<IWeeklyBriefProvider>();
            Assert.IsType<FakeWeeklyBriefProvider>(provider);

            var generate = scope.ServiceProvider
                .GetRequiredService<IWeeklyBriefGenerateService>();
            Assert.IsType<WeeklyBriefGenerateService>(generate);
        }
    }
}

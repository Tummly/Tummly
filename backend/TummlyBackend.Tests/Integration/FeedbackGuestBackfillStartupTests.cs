using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Data;
using TummlyBackend.Infrastructure;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Integration
{
    public sealed class GuestBackfillStartupWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public bool BackfillShouldFail { get; init; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Database:RunStartupInitInTests"] = "true",
                        ["Database:ApplyMigrationsOnStartup"] = "false",
                    }
                );
            });

            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d =>
                        d.ServiceType ==
                            typeof(DbContextOptions<ApplicationDbContext>)
                        || d.ServiceType == typeof(ApplicationDbContext)
                    )
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(w =>
                        w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                    );
                });

                if (BackfillShouldFail)
                {
                    services.RemoveAll<IFeedbackGuestBackfillService>();
                    services.AddScoped<
                        IFeedbackGuestBackfillService,
                        ThrowingFeedbackGuestBackfillService
                    >();
                }
            });
        }

        public async Task WaitForInitAsync(
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            timeout ??= TimeSpan.FromSeconds(10);
            var initState = Services.GetRequiredService<DatabaseInitState>();
            var deadline = DateTime.UtcNow + timeout.Value;

            while (DateTime.UtcNow < deadline)
            {
                var status = initState.Status;
                if (
                    status == DatabaseInitStatus.Succeeded
                    || status == DatabaseInitStatus.Failed
                )
                {
                    return;
                }

                await Task.Delay(50, cancellationToken);
            }

            throw new TimeoutException("Database initialization did not complete.");
        }

        private sealed class ThrowingFeedbackGuestBackfillService
            : IFeedbackGuestBackfillService
        {
            public Task BackfillAsync(CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Backfill failed for test.");
        }
    }

    public class FeedbackGuestBackfillStartupTests
    {
        [Fact]
        public async Task HealthReady_returns_ok_after_startup_backfill_succeeds()
        {
            using var factory = new GuestBackfillStartupWebApplicationFactory();
            using var client = factory.CreateClient();

            await factory.WaitForInitAsync();

            var response = await client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<HealthBody>();
            Assert.Equal("ready", body?.Status);
        }

        [Fact]
        public async Task HealthReady_returns_503_when_startup_backfill_fails()
        {
            using var factory = new GuestBackfillStartupWebApplicationFactory
            {
                BackfillShouldFail = true,
            };
            using var client = factory.CreateClient();

            await factory.WaitForInitAsync();

            var initState = factory.Services.GetRequiredService<DatabaseInitState>();
            Assert.Equal(DatabaseInitStatus.Failed, initState.Status);

            var response = await client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<HealthBody>();
            Assert.Equal("not_ready", body?.Status);
            Assert.Equal(
                "Database initialization failed",
                body?.Message
            );
        }

        private sealed class HealthBody
        {
            public string? Status { get; set; }

            public string? Message { get; set; }
        }
    }
}

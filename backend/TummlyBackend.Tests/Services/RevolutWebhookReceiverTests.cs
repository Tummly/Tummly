using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class RevolutWebhookReceiverTests
    {
        private const string SigningSecret = "test-webhook-signing-secret";
        private static readonly DateTimeOffset Now = new(
            2026,
            10,
            26,
            12,
            0,
            0,
            TimeSpan.Zero
        );

        [Fact]
        public async Task Receive_OldTimestamp_ReturnsBadSignature()
        {
            await using var provider = BuildProvider();
            const string body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_old"}""";
            var timestamp = Now.AddMinutes(-6).ToUnixTimeSeconds().ToString(
                CultureInfo.InvariantCulture
            );

            RevolutWebhookHandleResult result;
            await using (var scope = provider.CreateAsyncScope())
            {
                result = await scope.ServiceProvider
                    .GetRequiredService<IRevolutWebhookReceiver>()
                    .ReceiveAsync(body, Sign(body, timestamp), timestamp);
            }

            Assert.Equal(RevolutWebhookHandleStatus.BadSignature, result.Status);
            await using var assertScope = provider.CreateAsyncScope();
            var context = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Empty(context.RevolutWebhookInboxItems);
        }

        [Fact]
        public async Task Receive_FreshTimestamp_IsAccepted()
        {
            await using var provider = BuildProvider();
            const string body =
                """{"event":"ORDER_COMPLETED","order_id":"ord_fresh"}""";
            var timestamp = Now.AddMinutes(-4).ToUnixTimeMilliseconds().ToString(
                CultureInfo.InvariantCulture
            );

            RevolutWebhookHandleResult result;
            await using (var scope = provider.CreateAsyncScope())
            {
                result = await scope.ServiceProvider
                    .GetRequiredService<IRevolutWebhookReceiver>()
                    .ReceiveAsync(body, Sign(body, timestamp), timestamp);
            }

            Assert.Equal(RevolutWebhookHandleStatus.Accepted, result.Status);
            await using var assertScope = provider.CreateAsyncScope();
            var context = assertScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await context.RevolutWebhookInboxItems.CountAsync());
        }

        private static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName)
            );
            services.Configure<RevolutSettings>(settings =>
                settings.WebhookSigningSecret = SigningSecret
            );
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
            services.AddSingleton<IHostEnvironment>(
                new TestHostEnvironment("Production")
            );
            services.AddSingleton<IRevolutWebhookService, UnusedProcessor>();
            services.AddSingleton<IRevolutWebhookInboxWork, NoopInboxWork>();
            services.AddScoped<
                IRevolutWebhookReceiver,
                RevolutWebhookReceiver
            >();
            return services.BuildServiceProvider();
        }

        private static string Sign(string body, string timestamp) =>
            RevolutWebhookSignature.SignForTests(
                SigningSecret,
                timestamp,
                body
            );

        private sealed class UnusedProcessor : IRevolutWebhookService
        {
            public Task<RevolutWebhookHandleResult> HandleAsync(
                string rawBody,
                string? signatureHeader,
                string? requestTimestamp,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<RevolutWebhookHandleResult> ProcessVerifiedAsync(
                string rawBody,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();
        }

        private sealed class NoopInboxWork : IRevolutWebhookInboxWork
        {
            public ValueTask NotifyAsync(
                Guid inboxItemId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;

            public FixedTimeProvider(DateTimeOffset now)
            {
                _now = now;
            }

            public override DateTimeOffset GetUtcNow() => _now;
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } =
                new NullFileProvider();
        }
    }
}

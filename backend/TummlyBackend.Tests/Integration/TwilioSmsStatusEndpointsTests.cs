using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>POST /api/webhooks/twilio/sms-status</c> — Twilio status callback
    /// moves the guest reply with the matching SID (WS-E R3).
    /// </summary>
    public class TwilioSmsStatusEndpointsTests
    {
        private const string PublicBaseUrl = "https://api.tummly.test";
        private const string StatusPath = "/api/webhooks/twilio/sms-status";
        private const string SignedUrl = PublicBaseUrl + StatusPath;
        private const string RequestHostUrl = "http://localhost" + StatusPath;
        private const string Sid = "SM0123456789abcdef0123456789abcdef";

        private static readonly DateTime SeededAt = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        private static string AuthToken => TwilioSmsStatusEndpointsTestsAuth.Token;

        [Fact]
        public async Task StatusCallback_Delivered_SetsDelivered()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            var response = await PostSignedAsync(client, BuildForm(Sid, "delivered"));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var reply = await ReadReplyAsync(factory, Sid);
            Assert.Equal(GuestReplyDeliveryState.Delivered, reply.DeliveryState);
            Assert.True(reply.DeliveryStateAtUtc > SeededAt);
        }

        [Fact]
        public async Task StatusCallback_Undelivered_SetsFailed()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            var response = await PostSignedAsync(client, BuildForm(Sid, "undelivered"));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(
                GuestReplyDeliveryState.Failed,
                (await ReadReplyAsync(factory, Sid)).DeliveryState
            );
        }

        [Fact]
        public async Task StatusCallback_Failed_SetsFailed()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            var response = await PostSignedAsync(client, BuildForm(Sid, "failed"));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(
                GuestReplyDeliveryState.Failed,
                (await ReadReplyAsync(factory, Sid)).DeliveryState
            );
        }

        [Fact]
        public async Task StatusCallback_Sent_NoChange()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            var response = await PostSignedAsync(client, BuildForm(Sid, "sent"));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var reply = await ReadReplyAsync(factory, Sid);
            Assert.Equal(GuestReplyDeliveryState.Accepted, reply.DeliveryState);
            Assert.Equal(SeededAt, reply.DeliveryStateAtUtc);
        }

        [Fact]
        public async Task StatusCallback_RepeatAndLateSent_NoChange()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Delivered);

            var repeat = await PostSignedAsync(client, BuildForm(Sid, "delivered"));
            var late = await PostSignedAsync(client, BuildForm(Sid, "sent"));

            Assert.Equal(HttpStatusCode.NoContent, repeat.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, late.StatusCode);
            var reply = await ReadReplyAsync(factory, Sid);
            Assert.Equal(GuestReplyDeliveryState.Delivered, reply.DeliveryState);
            Assert.Equal(SeededAt, reply.DeliveryStateAtUtc);
        }

        [Fact]
        public async Task StatusCallback_BadSignature_403()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            using var request = BuildRequest(BuildForm(Sid, "delivered"));
            request.Headers.TryAddWithoutValidation(
                "X-Twilio-Signature",
                "not-a-valid-signature"
            );
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                GuestReplyDeliveryState.Accepted,
                (await ReadReplyAsync(factory, Sid)).DeliveryState
            );
        }

        [Fact]
        public async Task StatusCallback_MissingSignature_403()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            using var request = BuildRequest(BuildForm(Sid, "delivered"));
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                GuestReplyDeliveryState.Accepted,
                (await ReadReplyAsync(factory, Sid)).DeliveryState
            );
        }

        [Fact]
        public async Task StatusCallback_SignedAgainstRequestHost_403_WhenPublicBaseUrlSet()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            var response = await PostSignedAsync(
                client,
                BuildForm(Sid, "delivered"),
                RequestHostUrl
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(
                GuestReplyDeliveryState.Accepted,
                (await ReadReplyAsync(factory, Sid)).DeliveryState
            );
        }

        [Fact]
        public async Task StatusCallback_UnknownSid_204()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            var response = await PostSignedAsync(
                client,
                BuildForm("SMunknown0000000000000000000000000", "delivered")
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(
                GuestReplyDeliveryState.Accepted,
                (await ReadReplyAsync(factory, Sid)).DeliveryState
            );
        }

        [Fact]
        public async Task StatusCallback_SignedAgainstPublicBaseUrl_NotRequestHost()
        {
            await using var factory = new TwilioSmsStatusWebApplicationFactory();
            var client = factory.CreateClient();
            await SeedReplyAsync(factory, Sid, GuestReplyDeliveryState.Accepted);

            var response = await PostSignedAsync(
                client,
                BuildForm(Sid, "delivered"),
                SignedUrl
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(
                GuestReplyDeliveryState.Delivered,
                (await ReadReplyAsync(factory, Sid)).DeliveryState
            );
        }

        private static async Task<HttpResponseMessage> PostSignedAsync(
            HttpClient client,
            Dictionary<string, string> form,
            string signingUrl = SignedUrl
        )
        {
            var signature = TwilioSmsInboundEndpointsTests.ComputeTwilioSignature(
                AuthToken,
                signingUrl,
                form
            );
            using var request = BuildRequest(form);
            request.Headers.TryAddWithoutValidation("X-Twilio-Signature", signature);
            return await client.SendAsync(request);
        }

        private static HttpRequestMessage BuildRequest(Dictionary<string, string> form) =>
            new(HttpMethod.Post, StatusPath)
            {
                Content = new FormUrlEncodedContent(form),
            };

        private static Dictionary<string, string> BuildForm(string sid, string status) =>
            new()
            {
                ["MessageSid"] = sid,
                ["MessageStatus"] = status,
                ["AccountSid"] = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                ["From"] = "+447700900888",
                ["To"] = "+447911123456",
            };

        private static async Task SeedReplyAsync(
            WebApplicationFactory<Program> factory,
            string sid,
            GuestReplyDeliveryState state
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.FeedbackGuestResponses.Add(
                new FeedbackGuestResponse
                {
                    FeedbackId = 1,
                    Channel = FeedbackGuestResponseChannel.Sms,
                    Intent = FeedbackRecoveryIntent.RespondToGuest,
                    MaskedDestination = "••••3456",
                    Body = "Thanks for your feedback.",
                    AuthorDisplayName = "Operator",
                    ProviderMessageId = sid,
                    DeliveryState = state,
                    DeliveryStateAtUtc = SeededAt,
                }
            );
            await context.SaveChangesAsync();
        }

        private static async Task<FeedbackGuestResponse> ReadReplyAsync(
            WebApplicationFactory<Program> factory,
            string sid
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(row => row.ProviderMessageId == sid);
        }
    }

    internal sealed class TwilioSmsStatusWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                        ["PublicApi:BaseUrl"] = "https://api.tummly.test",
                        ["TwilioSettings:AuthToken"] = TwilioSmsStatusEndpointsTestsAuth.Token,
                        ["TwilioSettings:AccountSid"] = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                        ["TwilioSettings:RecoveryFromNumber"] = "+447700900888",
                        ["TwilioSettings:DefaultRegion"] = "GB",
                        [TummlySellerVatSettings.ModeActiveKey] = "true",
                        [TummlySellerVatSettings.RegistrationNumberKey] = "GB123456789",
                        [TummlySellerVatSettings.EffectiveDateKey] = "2024-01-01",
                        [TummlySellerVatSettings.LegalNameKey] = "Tummly Ltd",
                        [TummlySellerVatSettings.RegisteredAddressKey] = "1 Example Street",
                        ["Revolut:SecretKey"] = "sk_test_placeholder",
                        ["Revolut:WebhookSigningSecret"] = "whsec_placeholder",
                        ["Revolut:ApiBaseUrl"] = RevolutSettings.SandboxApiBaseUrl,
                        ["Revolut:ApiVersion"] = RevolutSettings.DefaultApiVersion,
                    }
                );
            });

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<TwilioSettings>(settings =>
                {
                    settings.AuthToken = TwilioSmsStatusEndpointsTestsAuth.Token;
                    settings.AccountSid = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                    settings.RecoveryFromNumber = "+447700900888";
                    settings.DefaultRegion = "GB";
                });

                var descriptors = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
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
            });
        }
    }

    /// <summary>Fake Twilio auth token for status callback tests.</summary>
    internal static class TwilioSmsStatusEndpointsTestsAuth
    {
        public const string Token = "twilio_test_auth_token_task5_status";
    }
}

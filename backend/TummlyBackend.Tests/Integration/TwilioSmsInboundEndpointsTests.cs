using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using Twilio.Security;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>POST /api/webhooks/twilio/sms-inbound</c> — Twilio STOP → SmsMarketing withdraw.
    /// </summary>
    public class TwilioSmsInboundEndpointsTests
    {
        private const string GuestFrom = "+447700900111";
        private const string WebhookPath = "/api/webhooks/twilio/sms-inbound";
        private const string WebhookUrl =
            "http://localhost/api/webhooks/twilio/sms-inbound";

        private static string AuthToken =>
            TwilioSmsInboundEndpointsTestsAuth.Token;

        private static string MappedTo =>
            TwilioSmsInboundEndpointsTestsAuth.MappedTo;

        [Fact]
        public async Task Inbound_BadSignature_Returns403_AndWritesNothing()
        {
            await using var factory = new TwilioSmsInboundWebApplicationFactory(
                mappedRestaurantId: 1
            );
            var client = factory.CreateClient();
            var seeded = await SeedGrantedSmsGuestAsync(
                factory,
                phoneE164: GuestFrom,
                restaurantName: "Sig Fail Venue"
            );

            var form = BuildForm(
                from: GuestFrom,
                to: MappedTo,
                body: "STOP"
            );
            using var content = ToFormContent(form);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                WebhookPath
            )
            {
                Content = content,
            };
            request.Headers.TryAddWithoutValidation(
                "X-Twilio-Signature",
                "not-a-valid-signature"
            );

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await AssertNoSmsWithdrawAsync(factory, seeded.LocationGuestId);
        }

        [Fact]
        public async Task Inbound_Stop_MappedTo_WithdrawsSmsMarketing()
        {
            await using var factory = new TwilioSmsInboundWebApplicationFactory(
                mappedRestaurantId: 1
            );
            var client = factory.CreateClient();
            var seeded = await SeedGrantedSmsGuestAsync(
                factory,
                phoneE164: GuestFrom,
                restaurantName: "Stop Venue"
            );
            Assert.Equal(1, seeded.RestaurantId);

            var form = BuildForm(
                from: GuestFrom,
                to: MappedTo,
                body: "STOP"
            );
            var response = await PostSignedAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "text/xml",
                response.Content.Headers.ContentType?.MediaType
            );

            using var scope = factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var states = await permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.SmsMarketing]
            );

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var entry = await context.LocationGuestPermissionLedgerEntries
                .SingleAsync(e =>
                    e.EventKind
                    == LocationGuestPermissionLedgerEventKinds.Withdraw
                );
            Assert.Equal(
                LocationGuestPermissionLedgerSources.SmsStop,
                entry.Source
            );
            Assert.Equal(
                LocationGuestPermissionKind.SmsMarketing,
                entry.PermissionKind
            );

            var restaurant = await context.Restaurants.SingleAsync();
            Assert.False(
                LocationGuestChannelPermissionGate.CanSendOnChannel(
                    restaurant,
                    states,
                    "sms"
                )
            );
        }

        [Fact]
        public async Task Inbound_NonStopBody_Returns200_WithoutWithdraw()
        {
            await using var factory = new TwilioSmsInboundWebApplicationFactory(
                mappedRestaurantId: 1
            );
            var client = factory.CreateClient();
            var seeded = await SeedGrantedSmsGuestAsync(
                factory,
                phoneE164: GuestFrom,
                restaurantName: "Hello Venue"
            );

            var form = BuildForm(
                from: GuestFrom,
                to: MappedTo,
                body: "HELLO"
            );
            var response = await PostSignedAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await AssertNoSmsWithdrawAsync(factory, seeded.LocationGuestId);
        }

        [Fact]
        public async Task Inbound_UnmappedTo_Returns200_WithoutWithdraw()
        {
            await using var factory = new TwilioSmsInboundWebApplicationFactory(
                mappedRestaurantId: 1
            );
            var client = factory.CreateClient();
            var seeded = await SeedGrantedSmsGuestAsync(
                factory,
                phoneE164: GuestFrom,
                restaurantName: "Unmapped Venue"
            );

            var form = BuildForm(
                from: GuestFrom,
                to: "+447700900000",
                body: "STOP"
            );
            var response = await PostSignedAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await AssertNoSmsWithdrawAsync(factory, seeded.LocationGuestId);
        }

        [Fact]
        public void TwilioSignatureHelper_MatchesRequestValidator()
        {
            var form = BuildForm(
                from: GuestFrom,
                to: MappedTo,
                body: "STOP"
            );
            var signature = ComputeTwilioSignature(AuthToken, WebhookUrl, form);
            var validator = new RequestValidator(AuthToken);
            Assert.True(validator.Validate(WebhookUrl, form, signature));
        }

        private static async Task<HttpResponseMessage> PostSignedAsync(
            HttpClient client,
            Dictionary<string, string> form
        )
        {
            var signature = ComputeTwilioSignature(
                AuthToken,
                WebhookUrl,
                form
            );
            using var content = ToFormContent(form);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                WebhookPath
            )
            {
                Content = content,
            };
            request.Headers.TryAddWithoutValidation(
                "X-Twilio-Signature",
                signature
            );
            return await client.SendAsync(request);
        }

        private static Dictionary<string, string> BuildForm(
            string from,
            string to,
            string body
        ) =>
            new()
            {
                ["From"] = from,
                ["To"] = to,
                ["Body"] = body,
                ["MessageSid"] = "SMaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                ["AccountSid"] = "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            };

        private static FormUrlEncodedContent ToFormContent(
            Dictionary<string, string> form
        ) => new(form);

        /// <summary>
        /// Twilio webhook HMAC-SHA1 signature (URL + sorted key/value concat).
        /// </summary>
        internal static string ComputeTwilioSignature(
            string authToken,
            string url,
            IDictionary<string, string> parameters
        )
        {
            var builder = new StringBuilder(url);
            foreach (
                var pair in parameters.OrderBy(
                    p => p.Key,
                    StringComparer.Ordinal
                )
            )
            {
                builder.Append(pair.Key);
                builder.Append(pair.Value);
            }

            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(authToken));
            var hash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(builder.ToString())
            );
            return Convert.ToBase64String(hash);
        }

        private static async Task AssertNoSmsWithdrawAsync(
            WebApplicationFactory<Program> factory,
            int locationGuestId
        )
        {
            using var scope = factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var states = await permissions.GetCurrentStatesAsync(
                locationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                states[LocationGuestPermissionKind.SmsMarketing]
            );

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.LocationGuestPermissionLedgerEntries.CountAsync(
                    e =>
                        e.EventKind
                        == LocationGuestPermissionLedgerEventKinds.Withdraw
                )
            );
        }

        private static async Task<SeededGuest> SeedGrantedSmsGuestAsync(
            WebApplicationFactory<Program> factory,
            string phoneE164,
            string restaurantName
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();

            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = $"{restaurantName} Main",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = phoneE164,
                NormalizedPhone = phoneE164,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(masterGuest);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                MasterGuest = masterGuest,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            var at = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            permissions.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );
            await context.SaveChangesAsync();

            return new SeededGuest(
                restaurant.Id,
                location.Id,
                guest.Id
            );
        }

        private sealed record SeededGuest(
            int RestaurantId,
            int LocationId,
            int LocationGuestId
        );
    }

    internal sealed class TwilioSmsInboundWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly int _mappedRestaurantId;

        public TwilioSmsInboundWebApplicationFactory(int mappedRestaurantId)
        {
            _mappedRestaurantId = mappedRestaurantId;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                        ["TwilioSettings:AuthToken"] =
                            TwilioSmsInboundEndpointsTestsAuth.Token,
                        ["TwilioSettings:AccountSid"] =
                            "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                        ["TwilioSettings:RecoveryFromNumber"] =
                            "+447700900888",
                        ["TwilioSettings:DefaultRegion"] = "GB",
                        [
                            $"TwilioSettings:InboundNumberRestaurants:{TwilioSmsInboundEndpointsTestsAuth.MappedTo}"
                        ] = _mappedRestaurantId.ToString(),
                        [TummlySellerVatSettings.ModeActiveKey] = "true",
                        [TummlySellerVatSettings.RegistrationNumberKey] =
                            "GB123456789",
                        [TummlySellerVatSettings.EffectiveDateKey] =
                            "2024-01-01",
                        [TummlySellerVatSettings.LegalNameKey] = "Tummly Ltd",
                        [TummlySellerVatSettings.RegisteredAddressKey] =
                            "1 Example Street",
                        ["Revolut:SecretKey"] = "sk_test_placeholder",
                        ["Revolut:WebhookSigningSecret"] = "whsec_placeholder",
                        ["Revolut:ApiBaseUrl"] =
                            RevolutSettings.SandboxApiBaseUrl,
                        ["Revolut:ApiVersion"] =
                            RevolutSettings.DefaultApiVersion,
                    }
                );
            });

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<TwilioSettings>(settings =>
                {
                    settings.AuthToken =
                        TwilioSmsInboundEndpointsTestsAuth.Token;
                    settings.AccountSid =
                        "ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                    settings.RecoveryFromNumber = "+447700900888";
                    settings.DefaultRegion = "GB";
                    settings.InboundNumberRestaurants[
                        TwilioSmsInboundEndpointsTestsAuth.MappedTo
                    ] = _mappedRestaurantId;
                });

                var descriptors = services
                    .Where(d =>
                        d.ServiceType
                            == typeof(DbContextOptions<ApplicationDbContext>)
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

    /// <summary>Shared auth/map constants for Twilio inbound webhook tests.</summary>
    internal static class TwilioSmsInboundEndpointsTestsAuth
    {
        public const string Token = "twilio_test_auth_token_task5";
        public const string MappedTo = "+447700900999";
    }
}

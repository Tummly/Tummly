using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>api/public/preferences/one-click</c> — anonymous RFC 8058 one-click unsubscribe.
    /// Authenticated only by the signed <c>t</c> token. Always 200.
    /// </summary>
    public class PublicGuestPreferencesOneClickTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string OneClickPath = "/api/public/preferences/one-click";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PublicGuestPreferencesOneClickTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task OneClick_WithdrawsEmailAtSenderLocationOnly()
        {
            const string sharedEmail = "one-click-shared@example.com";
            var sender = await SeedGrantedEmailGuestAsync(sharedEmail, "One Click Sender");
            var other = await SeedGrantedEmailGuestAsync(sharedEmail, "One Click Other");
            var token = CreateToken(sender.LocationGuestId, sender.RestaurantId);

            var response = await PostOneClickAsync(token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();

            var senderStates = await permissions.GetCurrentStatesAsync(sender.LocationGuestId);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                senderStates[LocationGuestPermissionKind.EmailMarketing]
            );

            var otherStates = await permissions.GetCurrentStatesAsync(other.LocationGuestId);
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                otherStates[LocationGuestPermissionKind.EmailMarketing]
            );
        }

        [Fact]
        public async Task OneClick_BadToken_200NoChange()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                "one-click-tamper@example.com",
                "One Click Tamper Venue"
            );
            var parts = CreateToken(seeded.LocationGuestId, seeded.RestaurantId).Split('.');
            var tampered = parts[0] + "x." + parts[1];

            var response = await PostOneClickAsync(tampered);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var states = await permissions.GetCurrentStatesAsync(seeded.LocationGuestId);
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                states[LocationGuestPermissionKind.EmailMarketing]
            );
        }

        [Fact]
        public async Task OneClick_EmptyBody_NoContentType_Returns200()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                "one-click-empty@example.com",
                "One Click Empty Venue"
            );
            var token = CreateToken(seeded.LocationGuestId, seeded.RestaurantId);
            var emptyBody = new ByteArrayContent(Array.Empty<byte>());
            emptyBody.Headers.ContentType = null;

            var response = await PostOneClickAsync(token, emptyBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var states = await permissions.GetCurrentStatesAsync(seeded.LocationGuestId);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.EmailMarketing]
            );
        }

        [Fact]
        public async Task OneClick_MultipartBody_200AndWithdraws()
        {
            // RFC 8058 receivers may POST multipart/form-data. The action reads only the query token.
            var seeded = await SeedGrantedEmailGuestAsync(
                "one-click-multipart@example.com",
                "One Click Multipart Venue"
            );
            var token = CreateToken(seeded.LocationGuestId, seeded.RestaurantId);
            var multipart = new MultipartFormDataContent
            {
                { new StringContent("One-Click"), "List-Unsubscribe" },
            };

            var response = await PostOneClickAsync(token, multipart);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();
            var states = await permissions.GetCurrentStatesAsync(seeded.LocationGuestId);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.EmailMarketing]
            );
        }

        [Fact]
        public async Task OneClick_IsNotRateLimited_WhenMailProviderRetriesFromOneIp()
        {
            // Mailbox providers POST from shared sending IPs. A 429 here would break the
            // "always 200" contract, so the one-click action is exempt from the guest-preferences
            // per-IP limit (20 per hour).
            var token = CreateToken(locationGuestId: 1, restaurantId: 1);

            for (var i = 0; i < GuestPreferencesRateLimit.PermitLimitPerIpPerHour + 5; i++)
            {
                var response = await PostOneClickAsync(token);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task OneClick_WithdrawThrows_StillReturns200()
        {
            var seeded = await SeedGrantedEmailGuestAsync(
                "one-click-throws@example.com",
                "One Click Throws Venue"
            );
            var token = CreateToken(seeded.LocationGuestId, seeded.RestaurantId);
            using var host = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IGuestInitiatedMarketingWithdrawService>();
                    services.AddSingleton<IGuestInitiatedMarketingWithdrawService>(
                        new ThrowingWithdrawService()
                    );
                })
            );
            using var client = host.CreateClient();

            var response = await client.PostAsync(
                $"{OneClickPath}?t={Uri.EscapeDataString(token)}",
                RfcOneClickBody()
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private Task<HttpResponseMessage> PostOneClickAsync(
            string token,
            HttpContent? body = null
        ) =>
            _client.PostAsync(
                $"{OneClickPath}?t={Uri.EscapeDataString(token)}",
                body ?? RfcOneClickBody()
            );

        /// <summary>Withdraw fake that always fails, to prove the endpoint still answers 200.</summary>
        private sealed class ThrowingWithdrawService : IGuestInitiatedMarketingWithdrawService
        {
            public Task<GuestInitiatedWithdrawResult> WithdrawForLocationGuestAsync(
                int locationGuestId,
                int restaurantId,
                LocationGuestPermissionKind kind,
                string ledgerSource,
                CancellationToken cancellationToken = default
            ) => throw new InvalidOperationException("withdraw unavailable in test");

            public Task<GuestInitiatedWithdrawResult> WithdrawForRestaurantContactAsync(
                int restaurantId,
                string normalizedEmailOrE164,
                bool isEmail,
                LocationGuestPermissionKind kind,
                string ledgerSource,
                CancellationToken cancellationToken = default
            ) => throw new InvalidOperationException("withdraw unavailable in test");
        }

        /// <summary>RFC 8058 body sent by mailbox providers.</summary>
        private static HttpContent RfcOneClickBody() =>
            new FormUrlEncodedContent(
                new Dictionary<string, string> { ["List-Unsubscribe"] = "One-Click" }
            );

        private string CreateToken(int locationGuestId, int restaurantId)
        {
            using var scope = _factory.Services.CreateScope();
            return UnsubscribeToken.Create(
                locationGuestId,
                restaurantId,
                issuedAtUtc: DateTime.UtcNow,
                secret: GuestPreferencesEndpointTestSupport.UnsubscribeSecret(scope.ServiceProvider)
            );
        }

        private async Task<SeededGuest> SeedGrantedEmailGuestAsync(
            string email,
            string restaurantName
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var permissions = scope.ServiceProvider
                .GetRequiredService<ILocationGuestPermissionLedgerService>();

            var venue = await GuestPreferencesEndpointTestSupport.SeedVenueAsync(
                context,
                email,
                restaurantName,
                $"{restaurantName} Main"
            );

            permissions.RecordEvent(
                venue.Guest.Id,
                venue.Location.Id,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc)
            );
            await context.SaveChangesAsync();

            return new SeededGuest(venue.Restaurant.Id, venue.Guest.Id);
        }

        private sealed record SeededGuest(int RestaurantId, int LocationGuestId);
    }
}

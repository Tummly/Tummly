using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class GuestFormPermissionAndChannelEligibilityTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestFormPermissionAndChannelEligibilityTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetScan_ReturnsGuestFormConsentFromRestaurantTogglesAndWording()
        {
            const string token = "guest-form-consent-read";
            await SeedGuestLocationAsync(
                token,
                emailEnabled: true,
                smsEnabled: false,
                feedbackFollowUpEnabled: true,
                emailWording: "may email you about offers",
                smsWording: "unused"
            );

            var response = await _client.GetAsync($"/api/scan/{token}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var consent = body.GetProperty("guestFormConsent");
            Assert.True(consent.GetProperty("emailMarketingEnabled").GetBoolean());
            Assert.False(consent.GetProperty("smsMarketingEnabled").GetBoolean());
            Assert.True(
                consent.GetProperty("feedbackFollowUpEnabled").GetBoolean()
            );
            Assert.Equal(
                "may email you about offers",
                consent.GetProperty("emailConsentWording").GetString()
            );
            Assert.Equal(
                GuestFormConsentCopy.FeedbackFollowUpWording,
                consent.GetProperty("feedbackFollowUpWording").GetString()
            );
        }

        [Fact]
        public async Task SubmitFeedback_GrantsOnlyEnabledPermissionsOnConsent()
        {
            const string token = "guest-form-grant-enabled";
            await SeedGuestLocationAsync(
                token,
                emailEnabled: true,
                smsEnabled: false,
                feedbackFollowUpEnabled: true
            );

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Consent Guest",
                    guestContact = "consent@example.com",
                    comment = "Great visit.",
                    offersOptOut = false,
                }
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var locationId = await context.QrCodes
                .Where(q => q.Token == token)
                .Select(q => q.RestaurantLocationId)
                .SingleAsync();

            var locationGuest = await context.LocationGuests
                .Where(lg => lg.RestaurantLocationId == locationId)
                .SingleAsync();

            Assert.Equal(
                LocationGuestMarketingPreference.Allowed,
                locationGuest.MarketingPreference
            );

            var ledger = await context.LocationGuestPermissionLedgerEntries
                .Where(e => e.LocationGuestId == locationGuest.Id)
                .ToListAsync();

            Assert.Contains(
                ledger,
                e =>
                    e.PermissionKind == LocationGuestPermissionKind.EmailMarketing
                    && e.EventKind == LocationGuestPermissionLedgerEventKinds.Grant
                    && e.Source == LocationGuestPermissionLedgerSources.GuestForm
            );
            Assert.Contains(
                ledger,
                e =>
                    e.PermissionKind
                        == LocationGuestPermissionKind.FeedbackFollowUp
                    && e.EventKind == LocationGuestPermissionLedgerEventKinds.Grant
            );
            Assert.DoesNotContain(
                ledger,
                e => e.PermissionKind == LocationGuestPermissionKind.SmsMarketing
            );
        }

        [Fact]
        public async Task SubmitFeedback_WithdrawsMarketingPermissionsWhenOptingOut()
        {
            const string token = "guest-form-withdraw-marketing";
            await SeedGuestLocationAsync(
                token,
                emailEnabled: true,
                smsEnabled: true,
                feedbackFollowUpEnabled: true
            );

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Opt Out Guest",
                    guestContact = "optout@example.com",
                    comment = "Fine visit.",
                    offersOptOut = true,
                }
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var locationId = await context.QrCodes
                .Where(q => q.Token == token)
                .Select(q => q.RestaurantLocationId)
                .SingleAsync();

            var locationGuest = await context.LocationGuests
                .Where(lg => lg.RestaurantLocationId == locationId)
                .SingleAsync();
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                locationGuest.MarketingPreference
            );

            var ledger = await context.LocationGuestPermissionLedgerEntries
                .Where(e => e.LocationGuestId == locationGuest.Id)
                .ToListAsync();

            Assert.Equal(2, ledger.Count);
            Assert.All(
                ledger,
                e =>
                    Assert.Equal(
                        LocationGuestPermissionLedgerEventKinds.Withdraw,
                        e.EventKind
                    )
            );
            Assert.Contains(
                ledger,
                e => e.PermissionKind == LocationGuestPermissionKind.EmailMarketing
            );
            Assert.Contains(
                ledger,
                e => e.PermissionKind == LocationGuestPermissionKind.SmsMarketing
            );
            Assert.DoesNotContain(
                ledger,
                e => e.PermissionKind == LocationGuestPermissionKind.FeedbackFollowUp
            );
        }

        [Fact]
        public async Task CampaignEligibility_ExcludesGuestWhenEmailToggleOff()
        {
            var seeded = await SeedOwnerLocationWithGuestAsync(
                slug: "campaign-toggle-off",
                emailEnabled: false,
                smsEnabled: true,
                grantEmail: true,
                grantSms: true
            );

            using var request = AuthorizedGet(
                $"/api/campaigns/eligibility?locationId={seeded.LocationId}&audienceKey=all-eligible-guests",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var eligibility = body.GetProperty("eligibility");
            Assert.Equal(0, eligibility.GetProperty("emailEligible").GetInt32());
            Assert.Equal(1, eligibility.GetProperty("smsEligible").GetInt32());
        }

        [Fact]
        public async Task CampaignEligibility_ExcludesGuestWithoutChannelPermission()
        {
            var seeded = await SeedOwnerLocationWithGuestAsync(
                slug: "campaign-permission-off",
                emailEnabled: true,
                smsEnabled: true,
                grantEmail: false,
                grantSms: true
            );

            using var request = AuthorizedGet(
                $"/api/campaigns/eligibility?locationId={seeded.LocationId}&audienceKey=all-eligible-guests",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var eligibility = body.GetProperty("eligibility");
            Assert.Equal(0, eligibility.GetProperty("emailEligible").GetInt32());
            Assert.Equal(1, eligibility.GetProperty("smsEligible").GetInt32());
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task SeedGuestLocationAsync(
            string linkToken,
            bool emailEnabled,
            bool smsEnabled,
            bool feedbackFollowUpEnabled,
            string? emailWording = null,
            string? smsWording = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var restaurant = new Restaurant
            {
                Name = "Consent Venue",
                AccountType = "Single",
                OwnerUserId = 999_999,
                CreatedAt = now,
                EmailMarketingPermissionEnabled = emailEnabled,
                SmsMarketingPermissionEnabled = smsEnabled,
                FeedbackFollowUpPermissionEnabled = feedbackFollowUpEnabled,
                EmailConsentWording = emailWording,
                SmsConsentWording = smsWording,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.SmartGuest,
                    Token = linkToken,
                    Status = QrCodeStatus.Active,
                    CreatedAt = now,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task<OwnerSeed> SeedOwnerLocationWithGuestAsync(
            string slug,
            bool emailEnabled,
            bool smsEnabled,
            bool grantEmail,
            bool grantSms
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtFactory = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Owner",
                Email = $"{slug}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Campaign Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                EmailMarketingPermissionEnabled = emailEnabled,
                SmsMarketingPermissionEnabled = smsEnabled,
                FeedbackFollowUpPermissionEnabled = true,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"{slug}-guest@example.com",
                NormalizedEmail = $"{slug}-guest@example.com",
                Mobile = "07700900456",
                NormalizedPhone = "07700900456",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Campaign Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            var ledger = new LocationGuestPermissionLedgerService(context);
            var at = DateTime.UtcNow;

            if (grantEmail)
            {
                ledger.RecordEvent(
                    guest.Id,
                    location.Id,
                    LocationGuestPermissionKind.EmailMarketing,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    at
                );
            }
            else
            {
                ledger.RecordEvent(
                    guest.Id,
                    location.Id,
                    LocationGuestPermissionKind.EmailMarketing,
                    LocationGuestPermissionLedgerEventKinds.Withdraw,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    at
                );
            }

            if (grantSms)
            {
                ledger.RecordEvent(
                    guest.Id,
                    location.Id,
                    LocationGuestPermissionKind.SmsMarketing,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    at
                );
            }
            else
            {
                ledger.RecordEvent(
                    guest.Id,
                    location.Id,
                    LocationGuestPermissionKind.SmsMarketing,
                    LocationGuestPermissionLedgerEventKinds.Withdraw,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    at
                );
            }

            await ledger.SyncMarketingPreferenceRollupAsync(guest.Id);
            await context.SaveChangesAsync();

            return new OwnerSeed(
                jwtFactory.GenerateToken(
                    user.Id.ToString(),
                    user.Email,
                    user.Role
                ),
                location.Id
            );
        }

        private sealed record OwnerSeed(string Jwt, int LocationId);
    }
}

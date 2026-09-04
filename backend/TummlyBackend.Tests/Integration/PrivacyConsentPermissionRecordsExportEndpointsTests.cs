using System.Net;
using System.Net.Http.Headers;
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
    /// <summary>
    /// Seam: <c>GET /api/privacy-consent/permission-records/export</c> — auth,
    /// Soft-lock/Dormant/chargeback 403 (list stays 200), empty success, location
    /// scope (no date window), soft-max, Content-Disposition filename.
    /// </summary>
    public class PrivacyConsentPermissionRecordsExportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PrivacyConsentPermissionRecordsExportEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Export_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(ExportUrl(1));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Export_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("pcex-ownera");
            var other = await SeedOwnerAsync("pcex-ownerb");

            using var request = AuthorizedGet(
                ExportUrl(other.LocationId),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Export_Returns403_WhenCallerLacksPrivacyConsentView()
        {
            var seeded = await SeedOwnerAsync("pcex-no-privacy-view");
            var memberJwt = await SeedMemberJwtAsync(
                seeded.RestaurantId,
                "pcex-reporting-only",
                PermissionRoles.ReportingOnly
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId),
                memberJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Export_Empty_Returns200WithHeadersAndDisposition()
        {
            var seeded = await SeedOwnerAsync("pcex-empty");

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType
            );

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-consent-permission-records-{seeded.LocationId}-",
                fileName
            );
            Assert.EndsWith("Z.csv", fileName);

            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("Guest", csv);
            Assert.Contains("Permission", csv);
            Assert.Contains("Current state", csv);
            Assert.Contains("Location", csv);
            Assert.Contains("Source", csv);
            Assert.Contains("Recorded", csv);
            Assert.DoesNotContain("Action", csv);
            Assert.DoesNotContain("View", csv);

            var dataRows = csv
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .ToList();
            Assert.Empty(dataRows);
        }

        [Fact]
        public async Task Export_SoftLock_Returns403WithCode_ListStill200()
        {
            var seeded = await SeedOwnerAsync("pcex-soft", softLock: true);

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("soft_lock", json.GetProperty("code").GetString());

            using var listRequest = AuthorizedGet(
                ListUrl(),
                seeded.Jwt
            );
            var listResponse = await _client.SendAsync(listRequest);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        }

        [Fact]
        public async Task Export_Dormant_Returns403()
        {
            var seeded = await SeedOwnerAsync("pcex-dormant");
            await SetBillingStatusAsync(
                seeded.LocationId,
                BillingStatuses.Dormant
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("dormant", json.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Export_ChargebackRestricted_Returns403()
        {
            var seeded = await SeedOwnerAsync("pcex-cb");
            await SetChargebackRestrictedAsync(
                seeded.LocationId,
                restricted: true
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal(
                "chargeback_restricted",
                json.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task Export_LocationScope_IncludesAllRecordsForLocation_NoDateWindow()
        {
            var seeded = await SeedOwnerAsync("pcex-scope");
            var other = await SeedOwnerAsync("pcex-scope-other");
            var guestA = await SeedLocationGuestAsync(
                seeded.LocationId,
                "Guest A"
            );
            var guestB = await SeedLocationGuestAsync(
                seeded.LocationId,
                "Guest B"
            );
            var guestOther = await SeedLocationGuestAsync(
                other.LocationId,
                "Guest Other"
            );

            var oldStamp = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var recentStamp = new DateTime(
                2026,
                8,
                22,
                14,
                26,
                0,
                DateTimeKind.Utc
            );

            await SeedLedgerEntryAsync(
                guestA,
                seeded.LocationId,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                oldStamp
            );
            await SeedLedgerEntryAsync(
                guestB,
                seeded.LocationId,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.Operator,
                recentStamp
            );
            await SeedLedgerEntryAsync(
                guestOther,
                other.LocationId,
                LocationGuestPermissionKind.FeedbackFollowUp,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                recentStamp
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("Guest A", csv);
            Assert.Contains("Guest B", csv);
            Assert.Contains("Email marketing", csv);
            Assert.Contains("SMS marketing", csv);
            Assert.Contains("Granted", csv);
            Assert.Contains("Withdrawn", csv);
            Assert.DoesNotContain("Guest Other", csv);
            Assert.DoesNotContain("Feedback follow-up", csv);
        }

        [Fact]
        public async Task Export_SoftMax_ReturnsAtMost10000NewestRows()
        {
            var seeded = await SeedOwnerAsync("pcex-softmax");
            var baseAt = new DateTime(2026, 7, 10, 1, 0, 0, DateTimeKind.Utc);
            await SeedLedgerEntriesBulkAsync(
                seeded.LocationId,
                seeded.RestaurantId,
                count: 10_001,
                baseOccurredAt: baseAt
            );

            using var request = AuthorizedGet(
                ExportUrl(seeded.LocationId),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            var dataRows = csv
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .ToList();
            Assert.Equal(10_000, dataRows.Count);
            // Newest first: keep Guest-10000, drop oldest Guest-00000.
            Assert.Contains("Guest-10000", csv);
            Assert.DoesNotContain("Guest-00000", csv);
            Assert.Contains("Guest-10000", dataRows[0], StringComparison.Ordinal);
        }

        private static string ExportUrl(int locationId)
        {
            return $"/api/privacy-consent/permission-records/export?locationId={locationId}";
        }

        private static string ListUrl()
        {
            return "/api/privacy-consent/permission-records";
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

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId
        )> SeedOwnerAsync(string emailLocalPart, bool softLock = false)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Privacy Consent Export Owner",
                Email = $"{emailLocalPart}@example.com",
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
                Name = "Privacy Consent Export Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            if (softLock)
            {
                billing.BillingStatus = BillingStatuses.SoftLock;
                billing.SoftLockEnteredAt = DateTime.UtcNow.AddDays(-1);
            }

            context.BillingAccounts.Add(billing);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<string> SeedMemberJwtAsync(
            int restaurantId,
            string emailLocalPart,
            string permissionRole
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var member = new User
            {
                FullName = "Privacy Consent Export Member",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900999",
                Role = "Owner",
                AccountType = "Single",
                SelectedRestaurantId = restaurantId,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = member.Id,
                    RestaurantId = restaurantId,
                    PermissionRole = permissionRole,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );
        }

        private async Task SetBillingStatusAsync(
            int locationId,
            string billingStatus
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.RestaurantId)
                .FirstAsync();
            var account = await context.BillingAccounts
                .FirstAsync(a => a.RestaurantId == restaurantId);
            account.BillingStatus = billingStatus;
            if (billingStatus == BillingStatuses.Dormant)
            {
                account.DormantEnteredAt = DateTime.UtcNow.AddDays(-1);
            }

            await context.SaveChangesAsync();
        }

        private async Task SetChargebackRestrictedAsync(
            int locationId,
            bool restricted
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.RestaurantId)
                .FirstAsync();
            var account = await context.BillingAccounts
                .FirstAsync(a => a.RestaurantId == restaurantId);
            account.ChargebackRestricted = restricted;
            await context.SaveChangesAsync();
        }

        private async Task<int> SeedLocationGuestAsync(
            int locationId,
            string name
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var location = await context.RestaurantLocations
                .FindAsync(locationId);
            Assert.NotNull(location);

            var master = new MasterGuest
            {
                RestaurantId = location!.RestaurantId,
                Email = $"pcex-guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = name,
                CreatedAt = now,
            };
            context.LocationGuests.Add(lg);
            await context.SaveChangesAsync();
            return lg.Id;
        }

        private async Task SeedLedgerEntryAsync(
            int locationGuestId,
            int locationId,
            LocationGuestPermissionKind permissionKind,
            string eventKind,
            string source,
            DateTime occurredAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.LocationGuestPermissionLedgerEntries.Add(
                new LocationGuestPermissionLedgerEntry
                {
                    LocationGuestId = locationGuestId,
                    RestaurantLocationId = locationId,
                    PermissionKind = permissionKind,
                    EventKind = eventKind,
                    Source = source,
                    OccurredAt = occurredAt,
                    CreatedAt = occurredAt,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task SeedLedgerEntriesBulkAsync(
            int locationId,
            int restaurantId,
            int count,
            DateTime baseOccurredAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var masters = new List<MasterGuest>(count);
            for (var i = 0; i < count; i++)
            {
                masters.Add(
                    new MasterGuest
                    {
                        RestaurantId = restaurantId,
                        Email = $"pcex-sm-{i:D5}-{Guid.NewGuid():N}@example.com",
                        CreatedAt = baseOccurredAt,
                    }
                );
            }

            context.MasterGuests.AddRange(masters);
            await context.SaveChangesAsync();

            var guests = new List<LocationGuest>(count);
            for (var i = 0; i < count; i++)
            {
                guests.Add(
                    new LocationGuest
                    {
                        RestaurantLocationId = locationId,
                        MasterGuestId = masters[i].Id,
                        Name = $"Guest-{i:D5}",
                        CreatedAt = baseOccurredAt,
                    }
                );
            }

            context.LocationGuests.AddRange(guests);
            await context.SaveChangesAsync();

            var entries = new List<LocationGuestPermissionLedgerEntry>(count);
            for (var i = 0; i < count; i++)
            {
                entries.Add(
                    new LocationGuestPermissionLedgerEntry
                    {
                        LocationGuestId = guests[i].Id,
                        RestaurantLocationId = locationId,
                        PermissionKind =
                            LocationGuestPermissionKind.EmailMarketing,
                        EventKind =
                            LocationGuestPermissionLedgerEventKinds.Grant,
                        Source =
                            LocationGuestPermissionLedgerSources.GuestForm,
                        OccurredAt = baseOccurredAt.AddSeconds(i),
                        CreatedAt = baseOccurredAt.AddSeconds(i),
                    }
                );
            }

            context.LocationGuestPermissionLedgerEntries.AddRange(entries);
            await context.SaveChangesAsync();
        }
    }
}

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
    public class LocationsLifecycleEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationsLifecycleEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Pause_SetsPaused_PausesCapture_AndEmitsActivity()
        {
            var seeded = await SeedLifecycleScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/locations/{seeded.ActiveLocationId}/pause"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "paused",
                body.GetProperty("lifecycleStatus").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var location = await context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == seeded.ActiveLocationId);
            Assert.Equal(
                LocationLifecycleStatus.Paused,
                location.LifecycleStatus
            );
            Assert.Equal(
                CaptureLocationStatus.Paused,
                location.CaptureLocationStatus
            );

            var qr = await context.QrCodes
                .AsNoTracking()
                .FirstAsync(q => q.Id == seeded.ActiveQrId);
            Assert.Equal(QrCodeStatus.Paused, qr.Status);

            var activity = await context.LocationSettingsActivityEvents
                .AsNoTracking()
                .SingleAsync(e =>
                    e.LocationId == seeded.ActiveLocationId
                    && e.Kind
                        == LocationSettingsActivityKinds.LifecycleChanged
                );
            Assert.Contains("paused", activity.Description ?? string.Empty);
        }

        [Fact]
        public async Task Resume_SetsActive_AndActivatesCapture()
        {
            var seeded = await SeedLifecycleScenarioAsync();
            await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "pause"
            );

            var response = await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "resume"
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == seeded.ActiveLocationId);
            Assert.Equal(
                LocationLifecycleStatus.Active,
                location.LifecycleStatus
            );
            Assert.Equal(
                CaptureLocationStatus.Active,
                location.CaptureLocationStatus
            );
            var qr = await context.QrCodes
                .AsNoTracking()
                .FirstAsync(q => q.Id == seeded.ActiveQrId);
            Assert.Equal(QrCodeStatus.Active, qr.Status);
        }

        [Fact]
        public async Task Archive_FromPaused_Succeeds_FromActiveAndDraft_Refused()
        {
            var seeded = await SeedLifecycleScenarioAsync();

            var refuseActive = await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "archive"
            );
            Assert.Equal(HttpStatusCode.Conflict, refuseActive.StatusCode);

            var refuseDraft = await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.DraftLocationId,
                "archive"
            );
            Assert.Equal(HttpStatusCode.Conflict, refuseDraft.StatusCode);

            await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "pause"
            );
            var archive = await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "archive"
            );
            Assert.Equal(HttpStatusCode.OK, archive.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == seeded.ActiveLocationId);
            Assert.Equal(
                LocationLifecycleStatus.Archived,
                location.LifecycleStatus
            );
            Assert.Equal(
                CaptureLocationStatus.Paused,
                location.CaptureLocationStatus
            );
        }

        [Fact]
        public async Task Restore_FromArchived_LandsOnPaused()
        {
            var seeded = await SeedLifecycleScenarioAsync();
            await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "pause"
            );
            await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "archive"
            );

            var restore = await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "restore"
            );
            Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
            var body = await ReadJsonAsync(restore);
            Assert.Equal(
                "paused",
                body.GetProperty("lifecycleStatus").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .FirstAsync(l => l.Id == seeded.ActiveLocationId);
            Assert.Equal(
                LocationLifecycleStatus.Paused,
                location.LifecycleStatus
            );
        }

        [Fact]
        public async Task RestaurantLocations_HidesDraftAndArchived_IncludesLifecycleStatus()
        {
            var seeded = await SeedLifecycleScenarioAsync();
            await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "pause"
            );
            await PostLifecycleAsync(
                seeded.OwnerJwt,
                seeded.ActiveLocationId,
                "archive"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.OwnerJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var locations = body.GetProperty("locations").EnumerateArray()
                .ToList();

            Assert.DoesNotContain(
                locations,
                l => l.GetProperty("id").GetInt32() == seeded.DraftLocationId
            );
            Assert.DoesNotContain(
                locations,
                l => l.GetProperty("id").GetInt32() == seeded.ActiveLocationId
            );
            Assert.Contains(
                locations,
                l =>
                    l.GetProperty("id").GetInt32() == seeded.PausedLocationId
                    && l.GetProperty("lifecycleStatus").GetString() == "paused"
            );
        }

        [Fact]
        public async Task Pause_Returns403_ForViewOnlyMember()
        {
            var seeded = await SeedLifecycleScenarioAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/locations/{seeded.ActiveLocationId}/pause"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.ViewOnlyJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<HttpResponseMessage> PostLifecycleAsync(
            string jwt,
            int locationId,
            string action
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/locations/{locationId}/{action}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private async Task<LifecycleSeed> SeedLifecycleScenarioAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Lifecycle Owner",
                Email = $"loc-life-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900211",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Lifecycle Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                PrivacyConsentReadyAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = BillingCreditsService.CreateDefaultBillingAccount(
                    0,
                    "TUMMLY-UK-GBP-2026-08-V3"
                ),
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var active = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Active Life",
                Address = "1 High Street",
                City = "Camden",
                Postcode = "NW1 1AA",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CaptureLocationStatus = CaptureLocationStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            var draft = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Draft Life",
                Address = "2 High Street",
                City = "Soho",
                Postcode = "W1D 1AA",
                LifecycleStatus = LocationLifecycleStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };
            var paused = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Paused Life",
                Address = "3 High Street",
                City = "Camden",
                Postcode = "NW1 2BB",
                LifecycleStatus = LocationLifecycleStatus.Paused,
                CaptureLocationStatus = CaptureLocationStatus.Paused,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(active, draft, paused);
            await context.SaveChangesAsync();

            var qr = new QrCode
            {
                RestaurantLocationId = active.Id,
                QrType = QrType.CounterCard,
                Token = Guid.NewGuid().ToString("N")[..16],
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qr);

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );

            var viewer = new User
            {
                FullName = "Lifecycle Viewer",
                Email = $"loc-life-view-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900212",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(viewer);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = viewer.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.BillingAdmin,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();

            return new LifecycleSeed(
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateToken(
                    viewer.Id.ToString(),
                    viewer.Email,
                    viewer.Role
                ),
                active.Id,
                draft.Id,
                paused.Id,
                qr.Id
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private sealed record LifecycleSeed(
            string OwnerJwt,
            string ViewOnlyJwt,
            int ActiveLocationId,
            int DraftLocationId,
            int PausedLocationId,
            int ActiveQrId
        );
    }
}

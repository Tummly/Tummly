using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    public class ChecklistAcksEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ChecklistAcksEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetChecklistAcks_ReturnsFalseDefaults_ForOwnedLocation()
        {
            var seeded = await SeedOwnerLocationAsync(
                "checklist-get-token-123456789012"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/operator-home/checklist-acks?locationId={seeded.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                seeded.LocationId,
                body.GetProperty("locationId").GetInt32()
            );
            Assert.False(
                body.GetProperty("guestFormPreviewed").GetBoolean()
            );
            Assert.False(
                body.GetProperty("qrPlacementGuideViewed").GetBoolean()
            );
            Assert.False(body.GetProperty("logoUploaded").GetBoolean());
        }

        [Fact]
        public async Task PostChecklistAcks_SetsGuestFormPreviewed_Idempotently()
        {
            var seeded = await SeedOwnerLocationAsync(
                "checklist-post-token-12345678901"
            );

            using var postRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/operator-home/checklist-acks?locationId={seeded.LocationId}"
            );
            postRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            postRequest.Content = JsonContent.Create(
                new { guestFormPreviewed = true }
            );

            var postResponse = await _client.SendAsync(postRequest);

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.True(postBody.GetProperty("success").GetBoolean());
            Assert.True(
                postBody.GetProperty("guestFormPreviewed").GetBoolean()
            );
            Assert.False(
                postBody.GetProperty("qrPlacementGuideViewed").GetBoolean()
            );

            using var getRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/operator-home/checklist-acks?locationId={seeded.LocationId}"
            );
            getRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getResponse = await _client.SendAsync(getRequest);
            var getBody = await ReadJsonAsync(getResponse);

            Assert.True(
                getBody.GetProperty("guestFormPreviewed").GetBoolean()
            );

            using var secondPost = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/operator-home/checklist-acks?locationId={seeded.LocationId}"
            );
            secondPost.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            secondPost.Content = JsonContent.Create(
                new { guestFormPreviewed = true }
            );

            var secondResponse = await _client.SendAsync(secondPost);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            Assert.True(
                (await ReadJsonAsync(secondResponse))
                    .GetProperty("guestFormPreviewed")
                    .GetBoolean()
            );
        }

        [Fact]
        public async Task GetChecklistAcks_ReturnsLogoUploaded_WhenBrandLogoAlreadyPersisted()
        {
            var seeded = await SeedOwnerLocationAsync(
                "checklist-logo-heal-token-12345"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var location = Assert.Single(
                    context.RestaurantLocations.Where(
                        l => l.Id == seeded.LocationId
                    )
                );
                var restaurant = Assert.Single(
                    context.Restaurants.Where(
                        r => r.Id == location.RestaurantId
                    )
                );
                restaurant.BrandLogoObjectKey = "brand-logos/existing.png";
                restaurant.BrandLogoContentType = "image/png";
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/operator-home/checklist-acks?locationId={seeded.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("logoUploaded").GetBoolean());
        }

        [Fact]
        public async Task GetChecklistAcks_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerLocationAsync(
                "checklist-owner-token-1234567890"
            );
            var other = await SeedOwnerLocationAsync(
                "checklist-other-token-1234567890",
                email: "other-owner@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/operator-home/checklist-acks?locationId={other.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetChecklistAcks_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/operator-home/checklist-acks?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChecklistGet_UnassignedLocation_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await SendChecklistAsync(
                HttpMethod.Get,
                seeded.OutOfScopeLocationId,
                seeded.MemberJwt
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChecklistGet_StaffAssignedLocation_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Staff
            );

            var response = await SendChecklistAsync(
                HttpMethod.Get,
                seeded.InScopeLocationId,
                seeded.MemberJwt
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChecklistPost_UnassignedLocationManager_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await SendChecklistAsync(
                HttpMethod.Post,
                seeded.OutOfScopeLocationId,
                seeded.MemberJwt,
                new { guestFormPreviewed = true }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChecklistPost_Marketing_403()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.Marketing
            );

            var response = await SendChecklistAsync(
                HttpMethod.Post,
                seeded.InScopeLocationId,
                seeded.MemberJwt,
                new { guestFormPreviewed = true }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChecklistPost_AssignedManager_200()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await SendChecklistAsync(
                HttpMethod.Post,
                seeded.InScopeLocationId,
                seeded.MemberJwt,
                new { guestFormPreviewed = true }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("Owner", "GET", HttpStatusCode.OK)]
        [InlineData("Owner", "POST", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.Admin, "GET", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.Admin, "POST", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.AreaManager, "GET", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.AreaManager, "POST", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.LocationManager, "GET", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.LocationManager, "POST", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.Marketing, "GET", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.Marketing, "POST", HttpStatusCode.Forbidden)]
        [InlineData(PermissionRoles.ReportingOnly, "GET", HttpStatusCode.OK)]
        [InlineData(PermissionRoles.ReportingOnly, "POST", HttpStatusCode.Forbidden)]
        [InlineData(PermissionRoles.Staff, "GET", HttpStatusCode.Forbidden)]
        [InlineData(PermissionRoles.Staff, "POST", HttpStatusCode.Forbidden)]
        [InlineData(PermissionRoles.BillingAdmin, "GET", HttpStatusCode.Forbidden)]
        [InlineData(PermissionRoles.BillingAdmin, "POST", HttpStatusCode.Forbidden)]
        public async Task Checklist_RoleMatrix_UsesCaptureAreaOnAssignedLocation(
            string role,
            string method,
            HttpStatusCode expected
        )
        {
            // Owner rows use the seeded owner; the member role is unused there.
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                role == "Owner" ? PermissionRoles.Staff : role
            );
            var jwt = role == "Owner" ? seeded.OwnerJwt : seeded.MemberJwt;

            var response = await SendChecklistAsync(
                new HttpMethod(method),
                seeded.InScopeLocationId,
                jwt,
                method == "POST" ? new { guestFormPreviewed = true } : null
            );

            Assert.Equal(expected, response.StatusCode);
        }

        private async Task<HttpResponseMessage> SendChecklistAsync(
            HttpMethod method,
            int locationId,
            string jwt,
            object? body = null
        )
        {
            using var request = new HttpRequestMessage(
                method,
                $"/api/operator-home/checklist-acks?locationId={locationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await _client.SendAsync(request);
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerLocationAsync(
            string linkToken,
            string email = "checklist-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Checklist Owner",
                Email = email,
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
                Name = "Checklist Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

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

            return (jwt, location.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}

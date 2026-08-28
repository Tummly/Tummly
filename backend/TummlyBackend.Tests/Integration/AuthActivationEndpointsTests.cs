using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class AuthActivationEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthActivationEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UniversalLogin_AllowsSignIn_WhenActivationExpired()
        {
            var seeded = await SeedExpiredOperatorAsync();

            var response = await _client.PostAsJsonAsync(
                "/api/auth/universal-login",
                new
                {
                    email = "expired@example.com",
                    password = "password123",
                    deviceToken = seeded.DeviceToken,
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("USER", body.GetProperty("loginType").GetString());
            Assert.NotEqual(
                ActivationGate.ActivationExpiredMessage,
                body.TryGetProperty("message", out var message)
                    ? message.GetString()
                    : null
            );
        }

        [Fact]
        public async Task ProtectedOperatorRoute_Returns403_WhenPendingActivation()
        {
            var jwt = await SeedPendingOperatorAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("activationRequired").GetBoolean());
            Assert.Equal(
                "Account activation is required before accessing this resource.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task ProtectedOperatorRoute_Allows_WhenActivationExpiredMidSession()
        {
            var seeded = await SeedExpiredOperatorAsync();
            var jwt = seeded.Jwt;

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(
                body.TryGetProperty("activationExpired", out var expired)
                && expired.GetBoolean()
            );
        }

        [Fact]
        public async Task Me_IsAllowed_WhenPendingActivation()
        {
            var jwt = await SeedPendingOperatorAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/me"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.True(
                body.GetProperty("data")
                    .GetProperty("activationRequired")
                    .GetBoolean()
            );
        }

        [Fact]
        public async Task Me_Returns_SelfRole_From_Linked_Trial_Request_Without_Changing_Permission_Role()
        {
            var jwt = await SeedPendingOperatorWithTrialAsync(
                email: "self-role@example.com",
                selfRole: "owner-operator"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/me"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var data = body.GetProperty("data");

            Assert.Equal(
                "owner-operator",
                data.GetProperty("selfRole").GetString()
            );
            Assert.Equal(
                "Owner",
                data.GetProperty("role").GetString()
            );
        }

        [Fact]
        public async Task Activate_Succeeds_WhenPendingActivation()
        {
            var jwt = await SeedPendingOperatorAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/auth/activate"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(new
            {
                activationCode = "ABCD-2345",
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(
                body.GetProperty("data")
                    .GetProperty("activationRequired")
                    .GetBoolean()
            );
            Assert.NotEqual(
                JsonValueKind.Null,
                body.GetProperty("data")
                    .GetProperty("activationExpiresAt")
                    .ValueKind
            );
        }

        private async Task<string> SeedPendingOperatorAsync()
        {
            return await SeedPendingOperatorWithTrialAsync(
                email: "pending@example.com",
                selfRole: null
            );
        }

        private async Task<string> SeedPendingOperatorWithTrialAsync(
            string email,
            string? selfRole
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var user = new User
            {
                Email = email,
                FullName = "Pending Operator",
                PhoneNumber = "5551234568",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivationCodeHash =
                    ActivationCodeHelper.HashCode("ABCD2345"),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            if (selfRole != null)
            {
                context.TrialRequests.Add(
                    new TrialRequest
                    {
                        BusinessName = "Pending Cafe",
                        BusinessCategory = "cafe",
                        Locations = "1",
                        FullName = user.FullName,
                        Email = user.Email,
                        Mobile = user.PhoneNumber,
                        MainLocation = "1 High Street",
                        TownCity = "Leeds",
                        Postcode = "LS1 1AA",
                        Role = selfRole,
                        Goal = "Grow",
                        TermsAccepted = true,
                        IsEmailVerified = true,
                        IsApproved = true,
                        IsAccountCreated = true,
                        AccountCreatedAt = DateTime.UtcNow,
                        AccountType = "Single",
                        Status = TrialRequestStatus.InviteSent,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }

            var restaurant = new Restaurant
            {
                Name = "Pending Cafe",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private async Task<ExpiredSeed> SeedExpiredOperatorAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var user = new User
            {
                Email = "expired@example.com",
                FullName = "Expired Operator",
                PhoneNumber = "5551234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivatedAt = DateTime.UtcNow.AddDays(-40),
                ActivationExpiresAt = DateTime.UtcNow.AddDays(-1),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Expired Cafe",
                AccountType = "Single",
                OwnerUserId = user.Id,
                BillingContactUserId = user.Id,
                PrivacyContactUserId = user.Id,
                SupportContactUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            user.SelectedRestaurantId = restaurant.Id;
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = user.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            billing.PilotPeriodEnd = user.ActivationExpiresAt;
            context.BillingAccounts.Add(billing);

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            var deviceToken = await TrustedDeviceHelper.IssueTrustedDeviceAsync(
                context,
                user.Id
            );

            await context.SaveChangesAsync();

            return new ExpiredSeed(
                jwtService.GenerateToken(
                    user.Id.ToString(),
                    user.Email,
                    user.Role
                ),
                deviceToken!
            );
        }

        private sealed record ExpiredSeed(string Jwt, string DeviceToken);

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}

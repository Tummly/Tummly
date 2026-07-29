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
        public async Task UniversalLogin_ReturnsTrialOverMessage_ForExpiredOperator()
        {
            await SeedExpiredOperatorAsync();

            var response = await _client.PostAsJsonAsync(
                "/api/auth/universal-login",
                new
                {
                    email = "expired@example.com",
                    password = "password123",
                }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                ActivationGate.ActivationExpiredMessage,
                body.GetProperty("message").GetString()
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
        public async Task ProtectedOperatorRoute_Returns403_WhenActivationExpiredMidSession()
        {
            var jwt = await SeedExpiredOperatorAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("activationExpired").GetBoolean());
            Assert.Equal(
                ActivationGate.ActivationExpiredMessage,
                body.GetProperty("message").GetString()
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

        private async Task<string> SeedExpiredOperatorAsync()
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

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}

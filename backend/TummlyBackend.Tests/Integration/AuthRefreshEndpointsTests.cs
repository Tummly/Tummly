using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AuthRefreshEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthRefreshEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Refresh_IssuesNewAccessToken_ForValidRefreshToken()
        {
            var issued = await SeedOperatorWithRefreshTokenAsync();

            var refreshResponse = await _client.PostAsJsonAsync(
                "/api/auth/refresh",
                new { refreshToken = issued.RefreshToken }
            );

            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

            var body = await ReadJsonAsync(refreshResponse);
            var data = body.GetProperty("data");
            var accessToken = data.GetProperty("token").GetString();
            var nextRefreshToken = data.GetProperty("refreshToken").GetString();

            Assert.False(string.IsNullOrWhiteSpace(accessToken));
            Assert.False(string.IsNullOrWhiteSpace(nextRefreshToken));
            Assert.NotEqual(issued.RefreshToken, nextRefreshToken);

            using var meRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/me"
            );
            meRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var meResponse = await _client.SendAsync(meRequest);

            Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        }

        [Fact]
        public async Task Refresh_Returns401_WhenRefreshTokenIsUnknown()
        {
            var response = await _client.PostAsJsonAsync(
                "/api/auth/refresh",
                new { refreshToken = "unknown-token" }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_Returns401_WhenAccessTokenIsExpired()
        {
            var expiredJwt = CreateExpiredAccessToken();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/auth/me"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", expiredJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<(int UserId, string RefreshToken)> SeedOperatorWithRefreshTokenAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new User
            {
                Email = "refresh-operator@example.com",
                FullName = "Refresh Operator",
                PhoneNumber = "5551234599",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Refresh Cafe",
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

            var refreshToken = await RefreshTokenHelper.IssueAsync(
                context,
                user.Id
            );
            await context.SaveChangesAsync();

            return (user.Id, refreshToken);
        }

        private static string CreateExpiredAccessToken()
        {
            var settings = new TummlyBackend.Configurations.JwtSettings
            {
                Secret = "integration-test-jwt-secret-32chars!",
                Issuer = "TummlyAPI",
                Audience = "TummlyClient",
                ExpiryMinutes = 60,
            };

            var now = DateTime.UtcNow;
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes(settings.Secret);

            var token = handler.CreateToken(
                new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(
                        new[]
                        {
                            new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.NameIdentifier,
                                "1"
                            ),
                            new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.Email,
                                "expired-jwt@example.com"
                            ),
                            new System.Security.Claims.Claim(
                                System.Security.Claims.ClaimTypes.Role,
                                "Owner"
                            ),
                        }
                    ),
                    NotBefore = now.AddMinutes(-10),
                    Expires = now.AddMinutes(-5),
                    Issuer = settings.Issuer,
                    Audience = settings.Audience,
                    SigningCredentials =
                        new Microsoft.IdentityModel.Tokens.SigningCredentials(
                            new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                            Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature
                        ),
                }
            );

            return handler.WriteToken(token);
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

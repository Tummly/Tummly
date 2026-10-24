using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Public product-demo JWT: writes are default-deny on the server (ENG07 07.1).
    /// Only the explicit allow-list runs; overlay writes are simulated.
    /// </summary>
    public class DemoSessionWriteAllowListTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string DemoOwnerEmail = "product-demo@example.com";
        private const string DemoDisabledMessage =
            "This action is disabled in the public product demo.";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DemoSessionWriteAllowListTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public static TheoryData<string, string> DeniedWrites =>
            new()
            {
                { "DELETE", "/api/guests/1" },
                { "POST", "/api/account/change-password" },
                { "PATCH", "/api/team-permissions/members/1/role" },
                { "POST", "/api/account-workspace/pause" },
                { "POST", "/api/campaigns/1/pause" },
                { "POST", "/api/billing-credits/top-up" },
                { "POST", "/api/shop/orders" },
                { "POST", "/api/assistant/stt" },
            };

        [Theory]
        [MemberData(nameof(DeniedWrites))]
        public async Task DemoJwt_WriteOutsideAllowList_Returns403(
            string method,
            string path
        )
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(new HttpMethod(method), path)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(DemoDisabledMessage, body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task DemoJwt_NotificationReadAll_IsSimulated()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/notifications/read-all"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("demoSessionIsolated").GetBoolean());
        }

        [Fact]
        public async Task DemoJwt_Logout_IsAllowed()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DemoJwt_AssistantTurn_WithOtherPrompt_Returns403()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns")
            {
                Content = JsonContent.Create(
                    new { message = "Write me a poem about the other visitors." }
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DemoJwt_AssistantTurn_WithTourPrompt_IsNotBlocked()
        {
            var token = await CreateDemoTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns")
            {
                Content = JsonContent.Create(
                    new { message = DemoSessionClaims.TourAssistantPrompt }
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DemoSession_IssuesNoRefreshToken()
        {
            await EnsureDemoOwnerAsync();

            var response = await _client.PostAsync("/api/auth/demo-session", content: null);

            var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.False(
                data.TryGetProperty("refreshToken", out var refresh)
                    && refresh.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(refresh.GetString())
            );
        }

        [Fact]
        public async Task Refresh_ForDemoOwner_IsRejected()
        {
            await EnsureDemoOwnerAsync();
            string refreshToken;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var owner = context.Users.Single(u => u.Email == DemoOwnerEmail);
                refreshToken = await RefreshTokenHelper.IssueAsync(context, owner.Id);
                await context.SaveChangesAsync();
            }

            var response = await _client.PostAsJsonAsync(
                "/api/auth/refresh",
                new { refreshToken }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task VerifyOtp_ForDemoOwner_IsRejected()
        {
            await EnsureDemoOwnerAsync();
            await SeedEmailOtpAsync(DemoOwnerEmail);

            var response = await _client.PostAsJsonAsync(
                "/api/auth/verify-otp",
                new { email = DemoOwnerEmail, otpCode = "123456" }
            );

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyOtp_ForDemoRestaurantStaff_IsRejected()
        {
            await EnsureDemoOwnerAsync();
            const string staffEmail = "demo-staff@example.com";
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!context.Users.Any(u => u.Email == staffEmail))
                {
                    var restaurantId = context.Restaurants
                        .Where(r => context.Users.Any(u => u.Id == r.OwnerUserId && u.Email == DemoOwnerEmail))
                        .Select(r => r.Id)
                        .First();
                    var staff = new User
                    {
                        Email = staffEmail,
                        FullName = "Demo Staff",
                        PhoneNumber = "5551234501",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                        Role = "Owner",
                        AccountType = "Single",
                        IsEmailVerified = true,
                        IsApprovedByAdmin = true,
                        TermsAccepted = true,
                        HasCompletedFirstSignIn = true,
                        SelectedRestaurantId = restaurantId,
                    };
                    context.Users.Add(staff);
                    await context.SaveChangesAsync();
                    context.RestaurantMemberships.Add(
                        new RestaurantMembership
                        {
                            UserId = staff.Id,
                            RestaurantId = restaurantId,
                            Status = MembershipStatus.Active,
                            PermissionRole = PermissionRoles.Staff,
                        }
                    );
                    await context.SaveChangesAsync();
                }
            }
            await SeedEmailOtpAsync(staffEmail);

            var response = await _client.PostAsJsonAsync(
                "/api/auth/verify-otp",
                new { email = staffEmail, otpCode = "123456" }
            );

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_ForSeedDemoOwner_IsRejected_EvenWithoutOwnerEmail()
        {
            await using var factory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?> { ["DemoSession:OwnerEmail"] = "" }
                    )
                )
            );
            string refreshToken;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var seedOwner = context.Users.FirstOrDefault(u => u.Email == DemoSessionClaims.SeedOwnerEmail);
                if (seedOwner == null)
                {
                    seedOwner = new User
                    {
                        Email = DemoSessionClaims.SeedOwnerEmail,
                        FullName = "Seed Demo Owner",
                        PhoneNumber = "5551234502",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                        Role = "Owner",
                        AccountType = "Single",
                        IsEmailVerified = true,
                        IsApprovedByAdmin = true,
                        TermsAccepted = true,
                        HasCompletedFirstSignIn = true,
                    };
                    context.Users.Add(seedOwner);
                    await context.SaveChangesAsync();
                }
                refreshToken = await RefreshTokenHelper.IssueAsync(context, seedOwner.Id);
                await context.SaveChangesAsync();
            }

            var response = await factory.CreateClient().PostAsJsonAsync(
                "/api/auth/refresh",
                new { refreshToken }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task SeedEmailOtpAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.OtpVerifications.Add(
                new OtpVerification
                {
                    Email = email,
                    OtpCode = "123456",
                    Channel = OtpVerification.ChannelEmail,
                    IsUsed = false,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task<string> CreateDemoTokenAsync()
        {
            await EnsureDemoOwnerAsync();

            var response = await _client.PostAsync("/api/auth/demo-session", content: null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("data").GetProperty("token").GetString()!;
        }

        private async Task EnsureDemoOwnerAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (context.Users.Any(u => u.Email == DemoOwnerEmail))
            {
                return;
            }

            var user = new User
            {
                Email = DemoOwnerEmail,
                FullName = "Product Demo Owner",
                PhoneNumber = "5551234500",
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
                Name = "Demo Kitchen",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            user.SelectedRestaurantId = restaurant.Id;

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Camden",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );

            await context.SaveChangesAsync();
        }
    }
}

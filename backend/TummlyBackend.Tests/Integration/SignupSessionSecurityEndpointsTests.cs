using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class SignupSessionSecurityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string SessionHeader = "X-Signup-Session";
        private const string ExpiredCode = "signup_session_expired";
        private const string ExpiredMessage =
            "Your signup session has expired. Enter your email to continue.";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SignupSessionSecurityEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Start_ResponseHasNoToken()
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailService>();
                    services.AddScoped<IEmailService, EmailServiceStubBase>();
                })
            );
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/signup/start",
                new { email = $"start-{Guid.NewGuid():N}@example.com", termsAccepted = true }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var raw = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("sessionToken", raw, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("code_sent", raw, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Session_NoHeader_401()
        {
            await AssertSessionExpiredAsync(await _client.GetAsync("/api/signup/session"));
        }

        [Fact]
        public async Task Session_UnknownToken_401()
        {
            await AssertSessionExpiredAsync(await GetSessionAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task Session_LegacyTokenNullIssued_401()
        {
            var token = await SeedRowAsync(PendingSignupStatuses.Verified, issuedAtUtc: null);

            await AssertSessionExpiredAsync(await GetSessionAsync(token));
        }

        [Fact]
        public async Task Session_Token25hOld_401()
        {
            var token = await SeedRowAsync(
                PendingSignupStatuses.Verified,
                issuedAtUtc: DateTime.UtcNow.AddHours(-25)
            );

            await AssertSessionExpiredAsync(await GetSessionAsync(token));
        }

        [Fact]
        public async Task Session_AbandonedRow_401()
        {
            var token = await SeedRowAsync(
                PendingSignupStatuses.Abandoned,
                issuedAtUtc: DateTime.UtcNow
            );

            await AssertSessionExpiredAsync(await GetSessionAsync(token));
        }

        [Fact]
        public async Task Session_CompleteRow_StatusOnly()
        {
            var token = await SeedRowAsync(
                PendingSignupStatuses.Complete,
                issuedAtUtc: DateTime.UtcNow,
                email: $"complete-{Guid.NewGuid():N}@example.com",
                fullName: "Complete Owner",
                onboardingJson: "{\"fullName\":\"Complete Owner\"}"
            );

            var response = await GetSessionAsync(token);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = body.RootElement.GetProperty("data");
            Assert.Equal(PendingSignupStatuses.Complete, data.GetProperty("status").GetString());
            Assert.False(data.TryGetProperty("sessionToken", out _));
            Assert.Equal(string.Empty, data.GetProperty("email").GetString());
            Assert.Equal(JsonValueKind.Null, data.GetProperty("onboardingJson").ValueKind);
            Assert.Equal(JsonValueKind.Null, data.GetProperty("fullName").ValueKind);
        }

        [Fact]
        public async Task SessionLogin_ClaimRevokesToken_SecondCall401()
        {
            var email = $"login-{Guid.NewGuid():N}@example.com";
            var token = await SeedRowAsync(
                PendingSignupStatuses.Complete,
                issuedAtUtc: DateTime.UtcNow,
                email: email,
                chosenPlan: BillingSubscriptionPlans.Pilot
            );
            await SeedPilotOwnerAsync(email);

            var first = await _client.PostAsJsonAsync(
                "/api/signup/session-login",
                new { sessionToken = token, continueOnFree = false }
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await _client.PostAsJsonAsync(
                "/api/signup/session-login",
                new { sessionToken = token, continueOnFree = false }
            );
            await AssertSessionExpiredAsync(second);
        }

        [Fact]
        public async Task Onboarding_QueryStringToken_401()
        {
            var token = await SeedRowAsync(
                PendingSignupStatuses.Verified,
                issuedAtUtc: DateTime.UtcNow
            );

            var response = await _client.PostAsJsonAsync(
                $"/api/signup/onboarding?sessionToken={token}",
                PilotOnboardingPayload()
            );

            await AssertSessionExpiredAsync(response);
        }

        [Fact]
        public async Task Onboarding_HeaderToken_Works()
        {
            var token = await SeedRowAsync(
                PendingSignupStatuses.Verified,
                issuedAtUtc: DateTime.UtcNow
            );

            var response = await PostOnboardingWithHeaderAsync(token, PilotOnboardingPayload());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task SignupToken_AsBearerOnAuthMe_401()
        {
            var token = await SeedRowAsync(
                PendingSignupStatuses.Verified,
                issuedAtUtc: DateTime.UtcNow
            );
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token.ToString()) },
            };

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnexpectedError_Returns500SignupError_NoExceptionText()
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ISignupService>();
                    services.AddScoped<ISignupService, ThrowingSignupService>();
                })
            );
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/signup/start",
                new { email = "boom@example.com", termsAccepted = true }
            );

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var raw = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(ThrowingSignupService.SecretText, raw, StringComparison.Ordinal);
            using var body = JsonDocument.Parse(raw);
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("signup_error", body.RootElement.GetProperty("code").GetString());
            Assert.Equal(
                "Something went wrong. Try again.",
                body.RootElement.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task Validation_PasswordsDoNotMatch_400_SameText()
        {
            var token = await SeedRowAsync(
                PendingSignupStatuses.Verified,
                issuedAtUtc: DateTime.UtcNow
            );
            var payload = PilotOnboardingPayload(confirmPassword: "Password2!");

            var response = await PostOnboardingWithHeaderAsync(token, payload);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("signup_invalid_request", body.RootElement.GetProperty("code").GetString());
            Assert.Equal(
                "Passwords do not match.",
                body.RootElement.GetProperty("message").GetString()
            );
        }

        private async Task<HttpResponseMessage> GetSessionAsync(Guid token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/signup/session");
            request.Headers.Add(SessionHeader, token.ToString());
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PostOnboardingWithHeaderAsync(
            Guid token,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/signup/onboarding")
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Add(SessionHeader, token.ToString());
            return await _client.SendAsync(request);
        }

        private static async Task AssertSessionExpiredAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(body.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(ExpiredCode, body.RootElement.GetProperty("code").GetString());
            Assert.Equal(ExpiredMessage, body.RootElement.GetProperty("message").GetString());
            Assert.False(body.RootElement.TryGetProperty("retryAfterUtc", out _));
        }

        private static object PilotOnboardingPayload(string confirmPassword = "Password1!") =>
            new
            {
                password = "Password1!",
                confirmPassword,
                fullName = "Pilot Owner",
                groupName = "Pilot Kitchen",
                businessCategory = "takeaway",
                primaryPhone = "07911123456",
                chosenPlan = BillingSubscriptionPlans.Pilot,
                chosenCadence = "monthly",
                locations = new[]
                {
                    new
                    {
                        locationName = "Main",
                        address = "1 High Street",
                        city = "Leeds",
                        postcode = "LS1 1AA",
                    },
                },
            };

        private async Task<Guid> SeedRowAsync(
            string status,
            DateTime? issuedAtUtc,
            string? email = null,
            string? fullName = null,
            string? onboardingJson = null,
            string? chosenPlan = null
        )
        {
            var sessionToken = Guid.NewGuid();
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    SessionTokenIssuedAtUtc = issuedAtUtc,
                    Email = email ?? $"signup-{Guid.NewGuid():N}@example.com",
                    Status = status,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    FullName = fullName,
                    OnboardingJson = onboardingJson,
                    ChosenPlan = chosenPlan,
                    EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await db.SaveChangesAsync();
            return sessionToken;
        }

        private async Task SeedPilotOwnerAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = new User
            {
                Email = email,
                FullName = "Owner",
                Role = "Owner",
                AccountType = "Single",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Kitchen",
                AccountType = "Single",
                OwnerUserId = user.Id,
                BusinessCategory = "takeaway",
            };
            db.Restaurants.Add(restaurant);
            await db.SaveChangesAsync();

            user.SelectedRestaurantId = restaurant.Id;
            db.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    SubscriptionPlan = BillingSubscriptionPlans.Pilot,
                    BillingStatus = BillingStatuses.Pilot,
                    ContractedPricebookId = "v3",
                }
            );
            await db.SaveChangesAsync();
        }

        private sealed class ThrowingSignupService : ISignupService
        {
            public const string SecretText = "boom-secret-text";

            private static Exception Boom() => new InvalidOperationException(SecretText);

            public Task<SignupCodeSentResponse> StartAsync(StartSignupDto dto) => throw Boom();

            public Task<SignupSessionResponse> VerifyOtpAsync(VerifySignupOtpDto dto) => throw Boom();

            public Task<SignupCodeSentResponse> ResendOtpAsync(string email) => throw Boom();

            public Task<SignupSessionResponse> SaveOnboardingAsync(
                Guid sessionToken,
                SaveSignupOnboardingDto dto
            ) => throw Boom();

            public Task<SignupResumeResponse> GetBySessionAsync(Guid sessionToken) => throw Boom();

            public Task RetryProvisionAsync(Guid sessionToken) => throw Boom();

            public Task<SignupProvisioningStatusResponse> GetProvisioningStatusAsync(
                Guid sessionToken
            ) => throw Boom();

            public Task<object> SessionLoginAsync(Guid sessionToken, bool continueOnFree = false) =>
                throw Boom();
        }
    }
}

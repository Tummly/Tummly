using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AccountWorkspaceEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AccountWorkspaceEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetDetails_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/account-workspace");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetDetails_ReturnsIdentityStatusAndShell()
        {
            var seeded = await SeedOwnerAsync(
                email: "aw-get@example.com",
                businessCategory: "takeaway",
                accountType: "Single"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/account-workspace"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Account Workspace Venue",
                body.GetProperty("workspaceName").GetString()
            );
            Assert.Equal(
                "Single location",
                body.GetProperty("accountStructure").GetString()
            );
            Assert.Equal(
                "takeaway",
                body.GetProperty("businessCategory").GetString()
            );
            Assert.Equal(
                "Takeaway / quick-service restaurant",
                body.GetProperty("businessCategoryLabel").GetString()
            );
            Assert.Equal(
                "United Kingdom",
                body.GetProperty("mainOperatingCountry").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("brandLogoOperatorUrl").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("brandLogoPublicUrl").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("lastSavedAt").ValueKind
            );

            var status = body.GetProperty("status");
            Assert.Equal("Active", status.GetProperty("workspaceStatus").GetString());
            Assert.Equal("Pilot", status.GetProperty("planStatus").GetString());
            Assert.Equal("Active", status.GetProperty("billingStatus").GetString());
            Assert.Equal(1, status.GetProperty("activeLocations").GetInt32());
            Assert.Equal(1, status.GetProperty("teamMembers").GetInt32());
            Assert.Equal(0, status.GetProperty("guestProfiles").GetInt32());
            Assert.Equal("Live", status.GetProperty("guestFormStatus").GetString());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    status.GetProperty("accountCreatedAt").GetString()
                )
            );
            Assert.False(
                string.IsNullOrWhiteSpace(
                    status.GetProperty("lastAccountUpdateAt").GetString()
                )
            );

            Assert.True(body.TryGetProperty("businessDetails", out _));
            Assert.True(body.TryGetProperty("keyContacts", out var keyContacts));
            Assert.Equal(JsonValueKind.Object, keyContacts.ValueKind);
            var accountOwner = keyContacts.GetProperty("accountOwner");
            Assert.Equal(
                "Account Workspace Owner",
                accountOwner.GetProperty("fullName").GetString()
            );
            Assert.Equal(
                "aw-get@example.com",
                accountOwner.GetProperty("email").GetString()
            );
            var ownerId = accountOwner.GetProperty("userId").GetInt32();
            Assert.Equal(
                ownerId,
                keyContacts.GetProperty("billingContactUserId").GetInt32()
            );
            Assert.Equal(
                ownerId,
                keyContacts.GetProperty("privacyContactUserId").GetInt32()
            );
            Assert.Equal(
                ownerId,
                keyContacts.GetProperty("supportContactUserId").GetInt32()
            );
            var eligible = keyContacts.GetProperty("eligibleMembers");
            Assert.Equal(1, eligible.GetArrayLength());
            Assert.Equal(
                ownerId,
                eligible[0].GetProperty("userId").GetInt32()
            );
            Assert.True(body.TryGetProperty("workspaceDefaults", out _));
        }

        [Fact]
        public async Task PutKeyContacts_HappyPath_PersistsWritableContacts()
        {
            var seeded = await SeedOwnerAsync(email: "aw-kc-ok@example.com");

            int ownerId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                ownerId = context.Restaurants
                    .Single(r => r.Id == seeded.RestaurantId)
                    .OwnerUserId;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/key-contacts"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                billingContactUserId = ownerId,
                privacyContactUserId = ownerId,
                supportContactUserId = ownerId,
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    body.GetProperty("lastSavedAt").GetString()
                )
            );
            var keyContacts = body.GetProperty("keyContacts");
            Assert.Equal(
                ownerId,
                keyContacts.GetProperty("billingContactUserId").GetInt32()
            );
            Assert.Equal(
                ownerId,
                keyContacts.GetProperty("privacyContactUserId").GetInt32()
            );
            Assert.Equal(
                ownerId,
                keyContacts.GetProperty("supportContactUserId").GetInt32()
            );

            using var verifyScope = _factory.Services.CreateScope();
            var verifyContext = verifyScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = Assert.Single(
                verifyContext.Restaurants.Where(r => r.Id == seeded.RestaurantId)
            );
            Assert.Equal(ownerId, restaurant.BillingContactUserId);
            Assert.Equal(ownerId, restaurant.PrivacyContactUserId);
            Assert.Equal(ownerId, restaurant.SupportContactUserId);
            Assert.NotNull(restaurant.AccountWorkspaceLastSavedAt);
        }

        [Fact]
        public async Task PutKeyContacts_RejectsAccountOwnerChange()
        {
            var seeded = await SeedOwnerAsync(email: "aw-kc-owner@example.com");

            int ownerId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                ownerId = context.Restaurants
                    .Single(r => r.Id == seeded.RestaurantId)
                    .OwnerUserId;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/key-contacts"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                billingContactUserId = ownerId,
                privacyContactUserId = ownerId,
                supportContactUserId = ownerId,
                accountOwnerUserId = ownerId + 999,
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "Account owner",
                body.GetProperty("message").GetString(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PutKeyContacts_RejectsUnknownMember()
        {
            var seeded = await SeedOwnerAsync(email: "aw-kc-unknown@example.com");

            int ownerId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                ownerId = context.Restaurants
                    .Single(r => r.Id == seeded.RestaurantId)
                    .OwnerUserId;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/key-contacts"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                billingContactUserId = ownerId + 50_000,
                privacyContactUserId = ownerId,
                supportContactUserId = ownerId,
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "eligible",
                body.GetProperty("message").GetString(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PutKeyContacts_RejectsMissingContactIds()
        {
            var seeded = await SeedOwnerAsync(email: "aw-kc-missing@example.com");

            int ownerId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                ownerId = context.Restaurants
                    .Single(r => r.Id == seeded.RestaurantId)
                    .OwnerUserId;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/key-contacts"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                billingContactUserId = 0,
                privacyContactUserId = ownerId,
                supportContactUserId = ownerId,
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "required",
                body.GetProperty("message").GetString(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PutKeyContacts_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PutAsJsonAsync(
                "/api/account-workspace/key-contacts",
                new
                {
                    billingContactUserId = 1,
                    privacyContactUserId = 1,
                    supportContactUserId = 1,
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PutAccountDetails_NameOnly_UpdatesNameAndLastSaved()
        {
            var seeded = await SeedOwnerAsync(email: "aw-put-name@example.com");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("  Renamed Workspace  "), "name");

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/account-details"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = content;

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Renamed Workspace",
                body.GetProperty("workspaceName").GetString()
            );
            Assert.False(
                string.IsNullOrWhiteSpace(
                    body.GetProperty("lastSavedAt").GetString()
                )
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = Assert.Single(
                context.Restaurants.Where(r => r.Id == seeded.RestaurantId)
            );
            Assert.Equal("Renamed Workspace", restaurant.Name);
            Assert.NotNull(restaurant.AccountWorkspaceLastSavedAt);
        }

        [Fact]
        public async Task PutAccountDetails_NameAndLogo_PersistsWhenStorageConfigured()
        {
            var storage = new InMemoryQueryAttachmentStorage();
            var client = CreateClientWithStorage(storage);
            var seeded = await SeedOwnerAsync(email: "aw-put-logo@example.com");

            var pngBytes = MinimalPngBytes();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Logo Venue"), "name");
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "logo", "brand.png");

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/account-details"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = content;

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Logo Venue",
                body.GetProperty("workspaceName").GetString()
            );
            var operatorUrl = body.GetProperty("brandLogoOperatorUrl").GetString();
            var publicUrl = body.GetProperty("brandLogoPublicUrl").GetString();
            Assert.False(string.IsNullOrWhiteSpace(operatorUrl));
            Assert.False(string.IsNullOrWhiteSpace(publicUrl));
            Assert.StartsWith("/api/public/brand-logos/", publicUrl);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = Assert.Single(
                context.Restaurants.Where(r => r.Id == seeded.RestaurantId)
            );
            Assert.False(string.IsNullOrWhiteSpace(restaurant.BrandLogoObjectKey));
            Assert.Equal("image/png", restaurant.BrandLogoContentType);
            Assert.Contains(restaurant.BrandLogoObjectKey!, storage.UploadedKeys);
        }

        [Fact]
        public async Task PutAccountDetails_WithLogo_FailsFastWhenStorageNotConfigured()
        {
            var seeded = await SeedOwnerAsync(email: "aw-put-nologo@example.com");

            var pngBytes = MinimalPngBytes();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Still Named"), "name");
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "logo", "brand.png");

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/account-details"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = content;

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = Assert.Single(
                context.Restaurants.Where(r => r.Id == seeded.RestaurantId)
            );
            Assert.Equal("Account Workspace Venue", restaurant.Name);
            Assert.Null(restaurant.BrandLogoObjectKey);
            Assert.Null(restaurant.AccountWorkspaceLastSavedAt);
        }

        [Fact]
        public async Task PutAccountDetails_Returns401_WhenUnauthenticated()
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Nope"), "name");

            var response = await _client.PutAsync(
                "/api/account-workspace/account-details",
                content
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


        [Fact]
        public async Task PutBusinessDetails_EmptyProfile_IsValidSave()
        {
            var seeded = await SeedOwnerAsync(email: "aw-biz-empty@example.com");

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/business-details"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new { });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(
                string.IsNullOrWhiteSpace(
                    body.GetProperty("lastSavedAt").GetString()
                )
            );

            var details = body.GetProperty("businessDetails");
            Assert.Equal(JsonValueKind.Object, details.ValueKind);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var row = Assert.Single(
                context.RestaurantBusinessDetails.Where(
                    d => d.RestaurantId == seeded.RestaurantId
                )
            );
            Assert.Null(row.LegalBusinessName);
            Assert.Null(row.TradingName);
            Assert.NotNull(
                context.Restaurants.Single(r => r.Id == seeded.RestaurantId)
                    .AccountWorkspaceLastSavedAt
            );
        }

        [Fact]
        public async Task PutBusinessDetails_SameAsLegal_CopiesTradingNameOnPersist()
        {
            var seeded = await SeedOwnerAsync(email: "aw-biz-same@example.com");

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/business-details"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                legalStructure = "limited-company",
                legalBusinessName = "Mehmet's Grill Ltd",
                tradingName = "Should Be Overwritten",
                sameAsLegalBusinessName = true,
                companyNumber = "12345678",
                vatNumber = "GB123",
                countryOfRegistration = "United Kingdom",
                addressLine1 = "1 High Street",
                addressLine2 = (string?)null,
                townCity = "London",
                county = "Greater London",
                postcode = "SW1A 1AA",
                country = "United Kingdom",
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var details = body.GetProperty("businessDetails");
            Assert.Equal(
                "Mehmet's Grill Ltd",
                details.GetProperty("legalBusinessName").GetString()
            );
            Assert.Equal(
                "Mehmet's Grill Ltd",
                details.GetProperty("tradingName").GetString()
            );
            Assert.False(details.TryGetProperty("sameAsLegalBusinessName", out _));

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurant = context.Restaurants.Single(
                r => r.Id == seeded.RestaurantId
            );
            Assert.Equal("Account Workspace Venue", restaurant.Name);
            var location = context.RestaurantLocations.Single(
                l => l.Id == seeded.LocationId
            );
            Assert.Equal("Main", location.LocationName);
            Assert.Equal("1 High Street", location.Address);
        }

        [Fact]
        public async Task PutBusinessDetails_UkPostcode_RejectsInvalidFormat()
        {
            var seeded = await SeedOwnerAsync(email: "aw-biz-postcode@example.com");

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/account-workspace/business-details"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new
            {
                country = "United Kingdom",
                postcode = "NOT A POSTCODE",
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutBusinessDetails_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PutAsJsonAsync(
                "/api/account-workspace/business-details",
                new { }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private HttpClient CreateClientWithStorage(
            IQueryAttachmentStorage storage
        )
        {
            return _factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var descriptors = services
                            .Where(d =>
                                d.ServiceType == typeof(IQueryAttachmentStorage)
                            )
                            .ToList();
                        foreach (var descriptor in descriptors)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddSingleton(storage);
                    });
                })
                .CreateClient();
        }

        private async Task<(
            string Jwt,
            int RestaurantId,
            int LocationId
        )> SeedOwnerAsync(
            string email = "aw-owner@example.com",
            string? businessCategory = "cafe",
            string accountType = "Single"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Account Workspace Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = accountType,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Account Workspace Venue",
                AccountType = accountType,
                OwnerUserId = user.Id,
                BillingContactUserId = user.Id,
                PrivacyContactUserId = user.Id,
                SupportContactUserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                BusinessCategory = businessCategory,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, restaurant.Id, location.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private static byte[] MinimalPngBytes()
        {
            // 1x1 transparent PNG
            return Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
            );
        }

        private sealed class InMemoryQueryAttachmentStorage
            : IQueryAttachmentStorage
        {
            private readonly Dictionary<string, (byte[] Bytes, string ContentType)>
                _objects = new();

            public bool IsConfigured => true;

            public HashSet<string> UploadedKeys { get; } = new();

            public Task UploadAsync(
                string storageKey,
                Stream content,
                string contentType,
                long contentLength,
                CancellationToken cancellationToken = default
            )
            {
                using var ms = new MemoryStream();
                content.CopyTo(ms);
                _objects[storageKey] = (ms.ToArray(), contentType);
                UploadedKeys.Add(storageKey);
                return Task.CompletedTask;
            }

            public Task<Stream> OpenReadAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                if (!_objects.TryGetValue(storageKey, out var stored))
                {
                    throw new FileNotFoundException(storageKey);
                }

                return Task.FromResult<Stream>(
                    new MemoryStream(stored.Bytes)
                );
            }

            public Task DeleteAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                _objects.Remove(storageKey);
                return Task.CompletedTask;
            }
        }
    }
}

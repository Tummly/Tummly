using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AdminPrintMaterialsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminPrintMaterialsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Ensure_ThenList_ReturnsReadyAssetsForThreeTypes()
        {
            var seeded = await SeedOperatorWithDefaultQrCodesAsync();

            using var ensure = AuthorizedPost(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/ensure",
                seeded.AdminJwt
            );
            var ensureResponse = await _client.SendAsync(ensure);
            Assert.Equal(HttpStatusCode.OK, ensureResponse.StatusCode);

            using var list = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/print-materials",
                seeded.AdminJwt
            );
            var listResponse = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            var body = await ReadJsonAsync(listResponse);
            var locations = body.GetProperty("data");
            Assert.Equal(1, locations.GetArrayLength());
            var assets = locations[0].GetProperty("assets");
            Assert.Equal(3, assets.GetArrayLength());
            foreach (var asset in assets.EnumerateArray())
            {
                Assert.Equal("Ready", asset.GetProperty("status").GetString());
            }

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await context.PrintReadyQrAssets
                .AsNoTracking()
                .Where(row => row.RestaurantLocationId == seeded.LocationId)
                .ToListAsync();
            Assert.Equal(3, stored.Count);
            Assert.All(stored, row =>
            {
                Assert.Equal(PrintReadyQrAssetStatus.Ready, row.Status);
                Assert.False(string.IsNullOrWhiteSpace(row.StorageKey));
            });
        }

        [Fact]
        public async Task Download_ReturnsPdf_WhenReady_WithDistinctTentAndStickerNames()
        {
            var seeded = await SeedOperatorWithDefaultQrCodesAsync();
            await EnsureAsync(seeded);

            using var tentRequest = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/TableTent/download",
                seeded.AdminJwt
            );
            var tentResponse = await _client.SendAsync(tentRequest);
            Assert.Equal(HttpStatusCode.OK, tentResponse.StatusCode);
            Assert.Equal(
                "application/pdf",
                tentResponse.Content.Headers.ContentType?.MediaType
            );
            var tentBytes = await tentResponse.Content.ReadAsByteArrayAsync();
            Assert.True(tentBytes.Length > 100);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(tentBytes[..4]));
            var tentName = tentResponse.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            Assert.Contains("table-tent", tentName, StringComparison.OrdinalIgnoreCase);

            using var stickerRequest = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/WindowSticker/download",
                seeded.AdminJwt
            );
            var stickerResponse = await _client.SendAsync(stickerRequest);
            Assert.Equal(HttpStatusCode.OK, stickerResponse.StatusCode);
            var stickerBytes = await stickerResponse.Content.ReadAsByteArrayAsync();
            var stickerName = stickerResponse.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            Assert.Contains("window-sticker", stickerName, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(tentName, stickerName);
            Assert.True(stickerBytes.Length > 100);

            using var cardRequest = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/OfferCard/download",
                seeded.AdminJwt
            );
            var cardResponse = await _client.SendAsync(cardRequest);
            Assert.Equal(HttpStatusCode.OK, cardResponse.StatusCode);
            var cardBytes = await cardResponse.Content.ReadAsByteArrayAsync();
            Assert.True(cardBytes.Length > 100);
            var cardText = System.Text.Encoding.ASCII.GetString(cardBytes);
            Assert.Contains("A thank-you offer for your next visit", cardText);
            Assert.DoesNotContain("10%", cardText);
            Assert.DoesNotContain("20%", cardText);
            Assert.DoesNotContain("50% off", cardText);
        }

        [Fact]
        public async Task Download_Returns403_ForOperatorJwt()
        {
            var seeded = await SeedOperatorWithDefaultQrCodesAsync();
            await EnsureAsync(seeded);

            using var request = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/TableTent/download",
                seeded.OwnerJwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Download_Denied_WhenFailedOrPreparing()
        {
            var seeded = await SeedOperatorWithDefaultQrCodesAsync();
            await InsertAssetAsync(
                seeded.LocationId,
                QrType.TableTent,
                PrintReadyQrAssetStatus.Preparing
            );
            await InsertAssetAsync(
                seeded.LocationId,
                QrType.WindowSticker,
                PrintReadyQrAssetStatus.Failed
            );

            using var preparing = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/TableTent/download",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await _client.SendAsync(preparing)).StatusCode
            );

            using var failed = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/WindowSticker/download",
                seeded.AdminJwt
            );
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await _client.SendAsync(failed)).StatusCode
            );
        }

        [Fact]
        public async Task Retry_Failed_BecomesReady_AndDownloadWorks()
        {
            var seeded = await SeedOperatorWithDefaultQrCodesAsync();
            await InsertAssetAsync(
                seeded.LocationId,
                QrType.OfferCard,
                PrintReadyQrAssetStatus.Failed,
                lastError: "boom"
            );

            using var retry = AuthorizedPost(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/OfferCard/retry",
                seeded.AdminJwt
            );
            var retryResponse = await _client.SendAsync(retry);
            Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
            var body = await ReadJsonAsync(retryResponse);
            Assert.Equal("Ready", body.GetProperty("data").GetProperty("status").GetString());

            using var download = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/OfferCard/download",
                seeded.AdminJwt
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(download)).StatusCode);
        }

        [Fact]
        public async Task SoftLock_DoesNotBlockAdminDownloadOrRetry()
        {
            var seeded = await SeedOperatorWithDefaultQrCodesAsync(
                billingStatus: BillingStatuses.SoftLock
            );
            await InsertAssetAsync(
                seeded.LocationId,
                QrType.TableTent,
                PrintReadyQrAssetStatus.Failed
            );

            using var retry = AuthorizedPost(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/TableTent/retry",
                seeded.AdminJwt
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(retry)).StatusCode);

            using var download = AuthorizedGet(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/TableTent/download",
                seeded.AdminJwt
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(download)).StatusCode);
        }

        [Fact]
        public async Task Ensure_DoesNotAlterQrCodeTokens()
        {
            var seeded = await SeedOperatorWithDefaultQrCodesAsync();
            var before = await ReadTokensAsync(seeded.LocationId);
            await EnsureAsync(seeded);
            var after = await ReadTokensAsync(seeded.LocationId);
            Assert.Equal(before, after);
        }

        private async Task EnsureAsync(Seeded seeded)
        {
            using var ensure = AuthorizedPost(
                $"/api/admin/operators/{seeded.OwnerUserId}/locations/{seeded.LocationId}/print-materials/ensure",
                seeded.AdminJwt
            );
            var response = await _client.SendAsync(ensure);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<Dictionary<QrType, string>> ReadTokensAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.QrCodes
                .AsNoTracking()
                .Where(row => row.RestaurantLocationId == locationId)
                .ToDictionaryAsync(row => row.QrType, row => row.Token);
        }

        private async Task InsertAssetAsync(
            int locationId,
            QrType qrType,
            PrintReadyQrAssetStatus status,
            string? lastError = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.PrintReadyQrAssets.Add(
                new PrintReadyQrAsset
                {
                    RestaurantLocationId = locationId,
                    QrType = qrType,
                    Status = status,
                    LastError = lastError,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task<Seeded> SeedOperatorWithDefaultQrCodesAsync(
            string billingStatus = BillingStatuses.Pilot
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Print Materials Owner",
                Email = $"owner-print-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900222",
                Role = "Owner",
                AccountType = "Single",
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
                Name = "Print Materials Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Front Room",
                Address = "2 High Street",
                City = "London",
                Postcode = "SE1 2AB",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

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
            owner.SelectedRestaurantId = restaurant.Id;

            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    BillingStatus = billingStatus,
                    ContractedPricebookId = "pricebook-v1",
                }
            );

            context.QrCodes.AddRange(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.TableTent,
                    Token = $"tent{Guid.NewGuid():N}"[..32],
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.WindowSticker,
                    Token = $"sticker{Guid.NewGuid():N}"[..32],
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                },
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.OfferCard,
                    Token = $"card{Guid.NewGuid():N}"[..32],
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );

            var tummlyAdmin = new Admin
            {
                FullName = "Tummly Admin",
                Email = $"admin-print-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.Add(tummlyAdmin);
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                location.Id,
                owner.Id,
                jwtService.GenerateToken(owner.Id.ToString(), owner.Email, owner.Role),
                jwtService.GenerateAdminToken(tummlyAdmin)
            );
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPost(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record Seeded(
            int RestaurantId,
            int LocationId,
            int OwnerUserId,
            string OwnerJwt,
            string AdminJwt
        );
    }
}

using System.Globalization;
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
    /// Seam: <c>GET /api/reports/export/{overview|capture|feedback|campaigns}</c>
    /// — auth, Soft-lock/Dormant/chargeback 403, empty-window success, filenames.
    /// </summary>
    public class ReportsExportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private static readonly DateTime WindowFrom = new(
            2026,
            7,
            10,
            0,
            0,
            0,
            DateTimeKind.Utc
        );
        private static readonly DateTime WindowTo = new(
            2026,
            7,
            17,
            0,
            0,
            0,
            DateTimeKind.Utc
        );

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReportsExportEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("overview")]
        [InlineData("capture")]
        [InlineData("feedback")]
        [InlineData("campaigns")]
        public async Task Export_Returns401_WhenUnauthenticated(string kind)
        {
            var response = await _client.GetAsync(
                ExportUrl(kind, 1, WindowFrom, WindowTo)
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("overview")]
        [InlineData("capture")]
        [InlineData("feedback")]
        [InlineData("campaigns")]
        public async Task Export_Returns403_ForNonOwnedLocation(string kind)
        {
            var owner = await SeedOwnerAsync($"rex-{kind}-ownera");
            var other = await SeedOwnerAsync($"rex-{kind}-ownerb");

            using var request = AuthorizedGet(
                ExportUrl(kind, other.LocationId, WindowFrom, WindowTo),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("overview", "pdf", "application/pdf")]
        [InlineData("capture", "csv", "text/csv")]
        [InlineData("feedback", "csv", "text/csv")]
        [InlineData("campaigns", "csv", "text/csv")]
        public async Task Export_EmptyWindow_Returns200WithDisposition(
            string kind,
            string extension,
            string mediaType
        )
        {
            var seeded = await SeedOwnerAsync($"rex-empty-{kind}");

            using var request = AuthorizedGet(
                ExportUrl(kind, seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                mediaType,
                response.Content.Headers.ContentType?.MediaType
            );

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-reports-{kind}-{seeded.LocationId}-",
                fileName
            );
            Assert.EndsWith($"Z.{extension}", fileName);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 0);

            if (extension == "csv")
            {
                var csv = await response.Content.ReadAsStringAsync();
                Assert.Contains("\n", csv);
            }
            else
            {
                var ascii = System.Text.Encoding.ASCII.GetString(bytes);
                Assert.Contains("%PDF", ascii);
            }
        }

        [Theory]
        [InlineData("overview", "soft_lock")]
        [InlineData("capture", "soft_lock")]
        [InlineData("feedback", "soft_lock")]
        [InlineData("campaigns", "soft_lock")]
        public async Task Export_SoftLock_Returns403WithCode(
            string kind,
            string expectedCode
        )
        {
            var seeded = await SeedOwnerAsync(
                $"rex-soft-{kind}",
                softLock: true
            );

            using var request = AuthorizedGet(
                ExportUrl(kind, seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal(expectedCode, json.GetProperty("code").GetString());

            using var kpiRequest = AuthorizedGet(
                OverviewUrl(seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var kpiResponse = await _client.SendAsync(kpiRequest);
            Assert.Equal(HttpStatusCode.OK, kpiResponse.StatusCode);
        }

        [Theory]
        [InlineData("overview")]
        [InlineData("capture")]
        [InlineData("feedback")]
        [InlineData("campaigns")]
        public async Task Export_Dormant_Returns403(string kind)
        {
            var seeded = await SeedOwnerAsync($"rex-dormant-{kind}");
            await SetBillingStatusAsync(
                seeded.LocationId,
                BillingStatuses.Dormant
            );

            using var request = AuthorizedGet(
                ExportUrl(kind, seeded.LocationId, WindowFrom, WindowTo),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("dormant", json.GetProperty("code").GetString());
        }

        [Theory]
        [InlineData("overview")]
        [InlineData("capture")]
        [InlineData("feedback")]
        [InlineData("campaigns")]
        public async Task Export_ChargebackRestricted_Returns403(string kind)
        {
            var seeded = await SeedOwnerAsync($"rex-cb-{kind}");
            await SetChargebackRestrictedAsync(
                seeded.LocationId,
                restricted: true
            );

            using var request = AuthorizedGet(
                ExportUrl(kind, seeded.LocationId, WindowFrom, WindowTo),
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

        private static string ExportUrl(
            string kind,
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/reports/export/{kind}?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string OverviewUrl(
            int locationId,
            DateTime from,
            DateTime to
        )
        {
            return $"/api/reports/overview?locationId={locationId}&from={Uri.EscapeDataString(FormatUtc(from))}&to={Uri.EscapeDataString(FormatUtc(to))}";
        }

        private static string FormatUtc(DateTime value)
        {
            return value
                .ToUniversalTime()
                .ToString(
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                    CultureInfo.InvariantCulture
                );
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

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string emailLocalPart,
            bool softLock = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Reports Export Owner",
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
                Name = "Reports Export Venue",
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

            return (jwt, location.Id);
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
    }
}

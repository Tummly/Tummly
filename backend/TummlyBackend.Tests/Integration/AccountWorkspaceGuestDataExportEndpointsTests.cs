using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class AccountWorkspaceGuestDataExportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private static readonly string ExpectedHeader =
            "Name,Email,Mobile,Location,Marketing preference,First captured";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AccountWorkspaceGuestDataExportEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Export_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/account-workspace/guest-data-export"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Export_Csv_ReturnsProfileAndConsentColumnsWithoutFeedbackBodies()
        {
            var seeded = await SeedRestaurantWithGuestsAsync(
                "aw-gde-csv@example.com"
            );

            using var request = AuthorizedGet(
                "/api/account-workspace/guest-data-export?format=csv",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType
            );

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-guest-data-{seeded.RestaurantId}-",
                fileName
            );
            Assert.EndsWith("Z.csv", fileName);

            var csv = await response.Content.ReadAsStringAsync();
            var lines = SplitCsvLines(csv);
            Assert.Equal(ExpectedHeader, lines[0]);
            Assert.Equal(3, lines.Count - 1);

            var jane = lines.Single(line => line.StartsWith("Jane Doe,"));
            Assert.Contains("jane@example.com", jane);
            Assert.Contains("Main", jane);
            Assert.Contains("Allowed", jane);
            Assert.Contains("2026-03-15T10:00:00.0000000Z", jane);

            var bob = lines.Single(line => line.StartsWith("Bob Harbour,"));
            Assert.Contains("07700900456", bob);
            Assert.Contains("Harbour", bob);
            Assert.Contains("Opted out", bob);

            var pat = lines.Single(line => line.StartsWith("Pat Silent,"));
            Assert.Contains("Not recorded", pat);

            Assert.DoesNotContain("SECRET_FEEDBACK_BODY", csv);
            Assert.DoesNotContain("Latest feedback", csv);
            Assert.DoesNotContain("Feedback submissions", csv);
        }

        [Fact]
        public async Task Export_DefaultFormat_ReturnsXlsxWorkbook()
        {
            var seeded = await SeedRestaurantWithGuestsAsync(
                "aw-gde-xlsx@example.com"
            );

            using var request = AuthorizedGet(
                "/api/account-workspace/guest-data-export",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                response.Content.Headers.ContentType?.MediaType
            );

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.EndsWith("Z.xlsx", fileName);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var cellTexts = ReadInlineStringsFromXlsx(bytes);
            Assert.Contains("Name", cellTexts);
            Assert.Contains("Marketing preference", cellTexts);
            Assert.Contains("Jane Doe", cellTexts);
            Assert.Contains("Bob Harbour", cellTexts);
            Assert.Contains("Allowed", cellTexts);
            Assert.DoesNotContain("SECRET_FEEDBACK_BODY", cellTexts);
        }

        [Fact]
        public async Task Export_RestaurantScope_ExcludesOtherRestaurantGuests()
        {
            var seeded = await SeedRestaurantWithGuestsAsync(
                "aw-gde-scope@example.com"
            );
            await SeedForeignRestaurantGuestAsync(
                "aw-gde-other@example.com"
            );

            using var request = AuthorizedGet(
                "/api/account-workspace/guest-data-export?format=csv",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("Jane Doe", csv);
            Assert.Contains("Bob Harbour", csv);
            Assert.DoesNotContain("Other Restaurant Guest", csv);
        }

        [Fact]
        public async Task Export_EmptyRestaurant_ReturnsHeaderOnly()
        {
            var seeded = await SeedOwnerAsync("aw-gde-empty@example.com");

            using var request = AuthorizedGet(
                "/api/account-workspace/guest-data-export?format=csv",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            var lines = SplitCsvLines(csv);
            Assert.Equal(new[] { ExpectedHeader }, lines);
        }

        [Fact]
        public async Task Export_InvalidFormat_Returns400()
        {
            var seeded = await SeedOwnerAsync("aw-gde-badfmt@example.com");

            using var request = AuthorizedGet(
                "/api/account-workspace/guest-data-export?format=pdf",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Export_DoesNotUpdateLastSavedAt()
        {
            var seeded = await SeedRestaurantWithGuestsAsync(
                "aw-gde-clock@example.com"
            );
            var lastSavedBefore = new DateTime(
                2026,
                4,
                1,
                12,
                0,
                0,
                DateTimeKind.Utc
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurant = context.Restaurants.Single(
                    r => r.Id == seeded.RestaurantId
                );
                restaurant.AccountWorkspaceLastSavedAt = lastSavedBefore;
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedGet(
                "/api/account-workspace/guest-data-export?format=csv",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurant = context.Restaurants.Single(
                    r => r.Id == seeded.RestaurantId
                );
                Assert.Equal(
                    lastSavedBefore,
                    restaurant.AccountWorkspaceLastSavedAt
                );
            }
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static List<string> SplitCsvLines(string csv)
        {
            return csv
                .Replace("\r\n", "\n")
                .TrimEnd('\n')
                .Split('\n')
                .ToList();
        }

        private static List<string> ReadInlineStringsFromXlsx(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
            Assert.NotNull(sheet);
            using var reader = new StreamReader(sheet!.Open(), Encoding.UTF8);
            var xml = reader.ReadToEnd();
            var doc = XDocument.Parse(xml);
            XNamespace ns =
                "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            return doc
                .Descendants(ns + "t")
                .Select(t => t.Value)
                .ToList();
        }

        private async Task<(
            string Jwt,
            int RestaurantId,
            int MainLocationId,
            int HarbourLocationId
        )> SeedOwnerAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Data Export Owner",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Guest Data Export Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                BillingContactUserId = user.Id,
                PrivacyContactUserId = user.Id,
                SupportContactUserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var main = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };
            var harbour = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Harbour",
                Address = "2 Quay Road",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };
            context.RestaurantLocations.AddRange(main, harbour);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, restaurant.Id, main.Id, harbour.Id);
        }

        private async Task<(
            string Jwt,
            int RestaurantId
        )> SeedRestaurantWithGuestsAsync(string email)
        {
            var owner = await SeedOwnerAsync(email);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var janeMaster = new MasterGuest
            {
                RestaurantId = owner.RestaurantId,
                Email = "jane@example.com",
                NormalizedEmail = "jane@example.com",
                CreatedAt = new DateTime(
                    2026,
                    3,
                    1,
                    9,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
            };
            var bobMaster = new MasterGuest
            {
                RestaurantId = owner.RestaurantId,
                Mobile = "07700900456",
                NormalizedPhone = "07700900456",
                CreatedAt = new DateTime(
                    2026,
                    3,
                    2,
                    9,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
            };
            var patMaster = new MasterGuest
            {
                RestaurantId = owner.RestaurantId,
                CreatedAt = new DateTime(
                    2026,
                    3,
                    3,
                    9,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
            };
            context.MasterGuests.AddRange(janeMaster, bobMaster, patMaster);
            await context.SaveChangesAsync();

            var jane = new LocationGuest
            {
                MasterGuestId = janeMaster.Id,
                RestaurantLocationId = owner.MainLocationId,
                Name = "Jane Doe",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = new DateTime(
                    2026,
                    3,
                    15,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
            };
            var bob = new LocationGuest
            {
                MasterGuestId = bobMaster.Id,
                RestaurantLocationId = owner.HarbourLocationId,
                Name = "Bob Harbour",
                MarketingPreference = LocationGuestMarketingPreference.OptedOut,
                CreatedAt = new DateTime(
                    2026,
                    3,
                    16,
                    11,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
            };
            var pat = new LocationGuest
            {
                MasterGuestId = patMaster.Id,
                RestaurantLocationId = owner.MainLocationId,
                Name = "Pat Silent",
                MarketingPreference =
                    LocationGuestMarketingPreference.NotRecorded,
                CreatedAt = new DateTime(
                    2026,
                    3,
                    17,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
            };
            context.LocationGuests.AddRange(jane, bob, pat);
            await context.SaveChangesAsync();

            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = owner.MainLocationId,
                    LocationGuestId = jane.Id,
                    GuestName = "Jane Doe",
                    GuestContact = "jane@example.com",
                    ContactType = ContactType.Email,
                    Comment = "SECRET_FEEDBACK_BODY",
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Positive,
                    CreatedAt = new DateTime(
                        2026,
                        3,
                        18,
                        8,
                        0,
                        0,
                        DateTimeKind.Utc
                    ),
                }
            );
            await context.SaveChangesAsync();

            return (owner.Jwt, owner.RestaurantId);
        }

        private async Task SeedForeignRestaurantGuestAsync(string email)
        {
            var other = await SeedOwnerAsync(email);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = other.RestaurantId,
                Email = "other@example.com",
                NormalizedEmail = "other@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            context.LocationGuests.Add(
                new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = other.MainLocationId,
                    Name = "Other Restaurant Guest",
                    MarketingPreference =
                        LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }
    }
}

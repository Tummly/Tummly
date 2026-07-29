using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestsExportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private static readonly string ExpectedHeader =
            "Name,Email,Mobile,Marketing status,Location,Latest feedback,"
            + "Feedback submissions,Last interaction,Last interaction at,"
            + "First captured,Guest tags";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestsExportEndpointsTests(
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
                "/api/guests/export?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Export_FullList_ReturnsCsvWithColumnsAndDisposition()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "export-full-token-1234567890"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ExportUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

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
                $"tummly-guests-{seeded.LocationId}-",
                fileName
            );
            Assert.EndsWith("Z.csv", fileName);
            Assert.DoesNotContain("-selected-", fileName);

            var csv = await response.Content.ReadAsStringAsync();
            var lines = SplitCsvLines(csv);
            Assert.Equal(ExpectedHeader, lines[0]);
            Assert.Equal(5, lines.Count - 1);

            var jane = lines.Single(line => line.StartsWith("Jane Doe,"));
            Assert.Contains("jane@example.com", jane);
            Assert.Contains("Eligible — Email", jane);
            Assert.Contains("Camden Street", jane);
            Assert.Contains("Positive", jane);
            Assert.Contains(",2,", jane);
            Assert.Contains("Feedback submitted", jane);
            Assert.Contains("Regular;VIP", jane);
        }

        [Fact]
        public async Task Export_FullList_EmptyMatch_ReturnsHeaderOnly()
        {
            var owner = await SeedOwnerAsync("export-empty-token-12345678");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(owner.LocationId)}&smartGroup=positive-feedback"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var csv = await response.Content.ReadAsStringAsync();
            var lines = SplitCsvLines(csv);
            Assert.Equal(new[] { ExpectedHeader }, lines);
        }

        [Fact]
        public async Task Export_FullList_AppliesFiltersAndSort_IgnoresPaging()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "export-filter-token-123456789"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(seeded.LocationId)}"
                    + "&marketing=eligible&sort=guest-name-az&page=1&pageSize=25"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var lines = SplitCsvLines(await response.Content.ReadAsStringAsync());
            var names = lines.Skip(1)
                .Select(line => line.Split(',')[0])
                .ToList();

            Assert.Equal(
                new[] { "Bob Mobile", "Jane Doe", "Old Pat" },
                names
            );
        }

        [Fact]
        public async Task Export_Selected_SingleGuestId_ReturnsSelectedFilenameAndOneRow()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "export-selected-n1-token-1234"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(seeded.LocationId)}"
                    + $"&guestIds={seeded.JaneLocationGuestId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-guests-selected-{seeded.LocationId}-",
                fileName
            );
            Assert.EndsWith("Z.csv", fileName);

            var lines = SplitCsvLines(await response.Content.ReadAsStringAsync());
            Assert.Equal(ExpectedHeader, lines[0]);
            Assert.Equal(2, lines.Count);
            Assert.StartsWith("Jane Doe,", lines[1]);
        }

        [Fact]
        public async Task Export_Selected_UsesGuestIdsOrder_IgnoresFilters()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "export-selected-token-1234567"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(seeded.LocationId)}"
                    + $"&guestIds={seeded.BobLocationGuestId}"
                    + $"&guestIds={seeded.JaneLocationGuestId}"
                    + "&marketing=not-opted-in&q=nope&smartGroup=dormant-guests"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-guests-selected-{seeded.LocationId}-",
                fileName
            );

            var lines = SplitCsvLines(await response.Content.ReadAsStringAsync());
            Assert.Equal(3, lines.Count);
            Assert.StartsWith("Bob Mobile,", lines[1]);
            Assert.StartsWith("Jane Doe,", lines[2]);
        }

        [Fact]
        public async Task Export_Selected_EmptyGuestIds_Returns400()
        {
            var owner = await SeedOwnerAsync("export-empty-ids-token-12345");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(owner.LocationId)}&guestIds="
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType ?? ""
            );
        }

        [Fact]
        public async Task Export_Selected_UnownedGuestId_FailsClosed()
        {
            var owner = await SeedOwnerAsync("export-unowned-a-token-123456");
            var other = await SeedGuestsScenarioAsync(
                "export-unowned-b-token-123456"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(owner.LocationId)}"
                    + $"&guestIds={other.JaneLocationGuestId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.DoesNotContain(
                "text/csv",
                response.Content.Headers.ContentType?.MediaType ?? ""
            );
        }

        [Fact]
        public async Task Export_Selected_IgnoresDeferredFiltersAndLocationOverride()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "export-selected-ignore-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(seeded.LocationId)}"
                    + $"&guestIds={seeded.JaneLocationGuestId}"
                    + "&recovery=open&locationScope=all"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var lines = SplitCsvLines(await response.Content.ReadAsStringAsync());
            Assert.Equal(2, lines.Count);
            Assert.StartsWith("Jane Doe,", lines[1]);
        }

        [Fact]
        public async Task Export_Selected_OverSoftMax_ThrowsNarrowFilters()
        {
            var owner = await SeedOwnerAsync("export-cap-selected-token-12");
            var oversizedIds = Enumerable.Range(1, 10_001).ToList();

            using var scope = _factory.Services.CreateScope();
            var export = scope.ServiceProvider
                .GetRequiredService<IGuestsExportService>();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                export.ExportAsync(
                    new DTOs.Guests.GuestsExportQuery
                    {
                        LocationIds = new[] { owner.LocationId },
                        LocationNamesById = new Dictionary<int, string>
                        {
                            [owner.LocationId] = "Camden Street",
                        },
                        ShellLocationId = owner.LocationId,
                        RestaurantId = owner.RestaurantId,
                        OwnerUserId = 0,
                        GuestIds = oversizedIds,
                        LocationScopeToken = owner.LocationId.ToString(),
                    }
                )
            );

            Assert.Contains(
                "narrow",
                ex.Message,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task Export_FullList_OverSoftMax_Returns400()
        {
            var seeded = await SeedManyGuestsAsync(
                "export-cap-full-token-123456",
                count: 10_001
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ExportUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Contains(
                "narrow",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task Export_FullList_LocationScopeAll_UsesAllFilenameToken()
        {
            var seeded = await SeedMultiLocationGuestsAsync(
                "export-scope-all-token-1234"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(seeded.LocationAId)}&locationScope=all"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith("tummly-guests-all-", fileName);

            var lines = SplitCsvLines(await response.Content.ReadAsStringAsync());
            Assert.Equal(2, lines.Count - 1);
            Assert.Contains(lines, line => line.Contains("Camden Street"));
            Assert.Contains(lines, line => line.Contains("Second Street"));
        }

        [Fact]
        public async Task Export_Selected_MultiLocation_UsesMultiFilenameToken()
        {
            var seeded = await SeedMultiLocationGuestsAsync(
                "export-scope-multi-token-123"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{ExportUrl(seeded.LocationAId)}"
                    + $"&guestIds={seeded.GuestAId}"
                    + $"&guestIds={seeded.GuestBId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith("tummly-guests-selected-multi-", fileName);
        }

        [Fact]
        public async Task Export_EscapesRfc4180Fields()
        {
            var owner = await SeedOwnerAsync("export-escape-token-12345678");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var master = new MasterGuest
                {
                    RestaurantId = owner.RestaurantId,
                    Email = "quote\"comma@example.com",
                    NormalizedEmail = "quote\"comma@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                context.LocationGuests.Add(
                    new LocationGuest
                    {
                        MasterGuestId = master.Id,
                        RestaurantLocationId = owner.LocationId,
                        Name = "Doe, \"Jane\"",
                        OffersOptOut = false,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ExportUrl(owner.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var csv = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"Doe, \"\"Jane\"\"\"", csv);
            Assert.Contains("\"quote\"\"comma@example.com\"", csv);
        }

        private static string ExportUrl(int locationId)
        {
            return $"/api/guests/export?locationId={locationId}";
        }

        private static List<string> SplitCsvLines(string csv)
        {
            return csv
                .Replace("\r\n", "\n")
                .TrimEnd('\n')
                .Split('\n')
                .ToList();
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId
        )> SeedOwnerAsync(
            string linkToken,
            string email = "export-owner@example.com",
            string locationName = "Camden Street"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Export Owner",
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
                Name = "Export Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
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

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<ExportScenarioSeed> SeedGuestsScenarioAsync(
            string linkToken
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: $"{linkToken}@example.com",
                locationName: "Camden Street"
            );
            var now = DateTime.UtcNow;

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            async Task<int> AddGuestAsync(
                string name,
                string? email,
                string? mobile,
                bool offersOptOut,
                DateTime capturedAt,
                IEnumerable<(
                    DateTime CreatedAt,
                    ClassificationStatus Status,
                    FeedbackSentiment? Sentiment
                )> feedbacks
            )
            {
                var master = new MasterGuest
                {
                    RestaurantId = owner.RestaurantId,
                    Email = email,
                    NormalizedEmail = email,
                    Mobile = mobile,
                    NormalizedPhone = mobile?.Replace(" ", ""),
                    CreatedAt = capturedAt,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = owner.LocationId,
                    Name = name,
                    OffersOptOut = offersOptOut,
                    CreatedAt = capturedAt,
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();

                foreach (var feedback in feedbacks)
                {
                    context.Feedbacks.Add(
                        new Feedback
                        {
                            RestaurantLocationId = owner.LocationId,
                            LocationGuestId = locationGuest.Id,
                            GuestName = name,
                            GuestContact = email ?? mobile ?? "unknown",
                            ContactType = email != null
                                ? ContactType.Email
                                : mobile != null
                                    ? ContactType.Phone
                                    : ContactType.Unknown,
                            Comment = "Visit note",
                            OffersOptOut = offersOptOut,
                            ClassificationStatus = feedback.Status,
                            Sentiment = feedback.Sentiment,
                            CreatedAt = feedback.CreatedAt,
                        }
                    );
                }

                await context.SaveChangesAsync();
                return locationGuest.Id;
            }

            var janeId = await AddGuestAsync(
                "Jane Doe",
                "jane@example.com",
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-5),
                feedbacks:
                [
                    (now.AddDays(-6), ClassificationStatus.Succeeded, FeedbackSentiment.Neutral),
                    (now.AddDays(-2), ClassificationStatus.Succeeded, FeedbackSentiment.Positive),
                ]
            );

            var bobId = await AddGuestAsync(
                "Bob Mobile",
                null,
                "07700 900456",
                offersOptOut: false,
                capturedAt: now.AddDays(-60),
                feedbacks:
                [
                    (now.AddDays(-10), ClassificationStatus.Pending, null),
                ]
            );

            await AddGuestAsync(
                "Old Pat",
                "pat@example.com",
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-200),
                feedbacks:
                [
                    (now.AddDays(-100), ClassificationStatus.Succeeded, FeedbackSentiment.Negative),
                ]
            );

            await AddGuestAsync(
                "Opt Out Sam",
                "sam@example.com",
                null,
                offersOptOut: true,
                capturedAt: now.AddDays(-10),
                feedbacks:
                [
                    (now.AddDays(-8), ClassificationStatus.Succeeded, FeedbackSentiment.Neutral),
                ]
            );

            await AddGuestAsync(
                "No Feedback",
                null,
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-2),
                feedbacks: Array.Empty<(DateTime, ClassificationStatus, FeedbackSentiment?)>()
            );

            var vip = new GuestTag
            {
                RestaurantId = owner.RestaurantId,
                DisplayName = "VIP",
                NormalizedName = "vip",
                AiSourced = false,
                CreatedAt = now,
            };
            var regular = new GuestTag
            {
                RestaurantId = owner.RestaurantId,
                DisplayName = "Regular",
                NormalizedName = "regular",
                AiSourced = false,
                CreatedAt = now,
            };
            context.GuestTags.AddRange(vip, regular);
            await context.SaveChangesAsync();

            context.LocationGuestTags.AddRange(
                new LocationGuestTag
                {
                    LocationGuestId = janeId,
                    GuestTagId = vip.Id,
                    CreatedAt = now,
                },
                new LocationGuestTag
                {
                    LocationGuestId = janeId,
                    GuestTagId = regular.Id,
                    CreatedAt = now,
                }
            );
            await context.SaveChangesAsync();

            return new ExportScenarioSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                janeId,
                bobId
            );
        }

        private async Task<(string Jwt, int LocationId)> SeedManyGuestsAsync(
            string linkToken,
            int count
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: $"{linkToken}@example.com"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var masters = new List<MasterGuest>(count);
            for (var i = 0; i < count; i++)
            {
                masters.Add(
                    new MasterGuest
                    {
                        RestaurantId = owner.RestaurantId,
                        Email = $"guest{i}@example.com",
                        NormalizedEmail = $"guest{i}@example.com",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    }
                );
            }

            context.MasterGuests.AddRange(masters);
            await context.SaveChangesAsync();

            var guests = masters
                .Select(
                    (master, index) => new LocationGuest
                    {
                        MasterGuestId = master.Id,
                        RestaurantLocationId = owner.LocationId,
                        Name = $"Guest {index}",
                        OffersOptOut = false,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-index),
                    }
                )
                .ToList();

            context.LocationGuests.AddRange(guests);
            await context.SaveChangesAsync();

            return (owner.Jwt, owner.LocationId);
        }

        private async Task<MultiLocationExportSeed> SeedMultiLocationGuestsAsync(
            string linkTokenPrefix
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Multi Export Owner",
                Email = $"{linkTokenPrefix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
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
                Name = "Multi Export Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var locationA = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var locationB = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Second Street",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(locationA, locationB);
            await context.SaveChangesAsync();

            var masterA = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "a@example.com",
                NormalizedEmail = "a@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            var masterB = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "b@example.com",
                NormalizedEmail = "b@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.AddRange(masterA, masterB);
            await context.SaveChangesAsync();

            var guestA = new LocationGuest
            {
                MasterGuestId = masterA.Id,
                RestaurantLocationId = locationA.Id,
                Name = "Location A Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
            };
            var guestB = new LocationGuest
            {
                MasterGuestId = masterB.Id,
                RestaurantLocationId = locationB.Id,
                Name = "Location B Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
            };
            context.LocationGuests.AddRange(guestA, guestB);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new MultiLocationExportSeed(
                jwt,
                locationA.Id,
                locationB.Id,
                guestA.Id,
                guestB.Id
            );
        }

        private sealed record ExportScenarioSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int JaneLocationGuestId,
            int BobLocationGuestId
        );

        private sealed record MultiLocationExportSeed(
            string Jwt,
            int LocationAId,
            int LocationBId,
            int GuestAId,
            int GuestBId
        );
    }
}

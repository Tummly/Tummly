using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    public class GuestContactMaskingEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestContactMaskingEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GuestsList_ReturnsMaskedContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var response = await GetAsync(
                seeded.OwnerJwt,
                $"/api/guests?locationId={seeded.InScopeLocationId}"
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var raw = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(seeded.GuestEmail, raw);
            Assert.DoesNotContain(seeded.GuestMobile, raw);

            var rows = JsonDocument.Parse(raw).RootElement
                .GetProperty("rows")
                .EnumerateArray()
                .ToList();

            var emailRow = rows.Single(row =>
                row.GetProperty("name").GetString() == "Email Guest"
            );
            Assert.Equal(
                GuestContactMask.MaskEmail(seeded.GuestEmail),
                emailRow.GetProperty("email").GetString()
            );
            Assert.Equal(
                "Eligible — Email",
                emailRow.GetProperty("marketingStatus").GetString()
            );

            var phoneRow = rows.Single(row =>
                row.GetProperty("name").GetString() == "Phone Guest"
            );
            Assert.Equal(
                GuestContactMask.MaskPhone(seeded.GuestMobile),
                phoneRow.GetProperty("mobile").GetString()
            );
        }

        [Fact]
        public async Task GuestsExport_ReturnsMaskedContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var csv = await GetCsvAsync(
                seeded.OwnerJwt,
                $"/api/guests/export?locationId={seeded.InScopeLocationId}"
            );

            AssertMaskedCsv(seeded, csv);
        }

        [Fact]
        public async Task Export_SelectedGuest_IsMasked()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var csv = await GetCsvAsync(
                seeded.OwnerJwt,
                $"/api/guests/export?locationId={seeded.InScopeLocationId}"
                    + $"&guestIds={seeded.EmailGuestId}"
            );

            Assert.Contains("Email Guest", csv);
            Assert.DoesNotContain(seeded.GuestEmail, csv);
            Assert.Contains(
                GuestContactMask.MaskEmail(seeded.GuestEmail)!,
                csv
            );
        }

        [Fact]
        public async Task GuestProfile_ReturnsMaskedContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var emailBody = await GetJsonAsync(
                seeded.OwnerJwt,
                $"/api/guests/{seeded.EmailGuestId}?locationId={seeded.InScopeLocationId}"
            );
            var emailSummary = emailBody.GetProperty("profileSummary");
            Assert.Equal(
                GuestContactMask.MaskEmail(seeded.GuestEmail),
                emailSummary.GetProperty("email").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                emailSummary.GetProperty("mobile").ValueKind
            );

            var phoneBody = await GetJsonAsync(
                seeded.OwnerJwt,
                $"/api/guests/{seeded.PhoneGuestId}?locationId={seeded.InScopeLocationId}"
            );
            var phoneSummary = phoneBody.GetProperty("profileSummary");
            Assert.Equal(
                GuestContactMask.MaskPhone(seeded.GuestMobile),
                phoneSummary.GetProperty("mobile").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                phoneSummary.GetProperty("email").ValueKind
            );
        }

        [Fact]
        public async Task GuestProfile_ContactEligibility_StillAvailable()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var body = await GetJsonAsync(
                seeded.OwnerJwt,
                $"/api/guests/{seeded.EmailGuestId}?locationId={seeded.InScopeLocationId}"
            );

            var eligibility = body.GetProperty("contactEligibility")
                .EnumerateArray()
                .ToList();
            var emailRow = eligibility.Single(row =>
                row.GetProperty("channel").GetString() == "email"
            );
            Assert.Equal("eligible", emailRow.GetProperty("status").GetString());
            Assert.NotEqual("not_provided", emailRow.GetProperty("status").GetString());
        }

        private async Task<HttpResponseMessage> GetAsync(string jwt, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task FeedbackRecent_ReturnsMaskedContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var raw = await GetCsvAsync(
                seeded.OwnerJwt,
                $"/api/feedback?locationId={seeded.InScopeLocationId}"
            );
            AssertNoRawContact(seeded, raw);

            var recent = JsonDocument.Parse(raw).RootElement
                .GetProperty("recent")
                .EnumerateArray()
                .ToList();

            var emailRow = recent.Single(row =>
                row.GetProperty("guestName").GetString() == "Email Guest"
            );
            Assert.Equal(
                GuestContactMask.MaskEmail(seeded.GuestEmail),
                emailRow.GetProperty("guestContact").GetString()
            );

            var phoneRow = recent.Single(row =>
                row.GetProperty("guestName").GetString() == "Phone Guest"
            );
            Assert.Equal(
                GuestContactMask.MaskPhone(seeded.GuestMobile),
                phoneRow.GetProperty("guestContact").GetString()
            );
        }

        [Fact]
        public async Task FeedbackDetail_ReturnsMaskedContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var emailRaw = await GetCsvAsync(
                seeded.OwnerJwt,
                $"/api/feedback/{seeded.EmailFeedbackId}"
            );
            AssertNoRawContact(seeded, emailRaw);
            Assert.Equal(
                GuestContactMask.MaskEmail(seeded.GuestEmail),
                JsonDocument.Parse(emailRaw).RootElement
                    .GetProperty("guestContact")
                    .GetString()
            );

            var phoneBody = await GetJsonAsync(
                seeded.OwnerJwt,
                $"/api/feedback/{seeded.PhoneFeedbackId}"
            );
            Assert.Equal(
                GuestContactMask.MaskPhone(seeded.GuestMobile),
                phoneBody.GetProperty("guestContact").GetString()
            );
        }

        [Fact]
        public async Task Export_AlwaysHasMaskedContactColumns()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var csv = await GetCsvAsync(
                seeded.OwnerJwt,
                FeedbackExportUrl(seeded.InScopeLocationId)
            );

            var header = csv.Split('\n', 2)[0].TrimEnd('\r');
            Assert.EndsWith(",Guest,Email,Mobile", header);
            AssertMaskedCsv(seeded, csv);
        }

        [Fact]
        public async Task Export_IncludeGuestContactTrue_StillMasked()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var csv = await GetCsvAsync(
                seeded.OwnerJwt,
                FeedbackExportUrl(
                    seeded.InScopeLocationId,
                    "&includeGuestContact=true"
                )
            );

            AssertMaskedCsv(seeded, csv);
        }

        [Fact]
        public async Task ExportSingle_IsMasked()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var emailCsv = await GetCsvAsync(
                seeded.OwnerJwt,
                $"/api/feedback/{seeded.EmailFeedbackId}/export"
                    + $"?locationId={seeded.InScopeLocationId}&format=csv"
            );
            AssertNoRawContact(seeded, emailCsv);
            Assert.Contains(
                GuestContactMask.MaskEmail(seeded.GuestEmail)!,
                emailCsv
            );

            var phoneCsv = await GetCsvAsync(
                seeded.OwnerJwt,
                $"/api/feedback/{seeded.PhoneFeedbackId}/export"
                    + $"?locationId={seeded.InScopeLocationId}&format=csv"
            );
            AssertNoRawContact(seeded, phoneCsv);
            Assert.Contains(
                GuestContactMask.MaskPhone(seeded.GuestMobile)!,
                phoneCsv
            );
        }

        [Fact]
        public async Task HomeLatestActivity_ReturnsMaskedContact()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            var raw = await GetCsvAsync(
                seeded.OwnerJwt,
                $"/api/home/latest-activity?locationId={seeded.InScopeLocationId}"
            );
            AssertNoRawContact(seeded, raw);

            var emailItem = JsonDocument.Parse(raw).RootElement
                .GetProperty("items")
                .EnumerateArray()
                .Single(item =>
                    item.GetProperty("kind").GetString() == "feedback"
                    && item.GetProperty("guestName").GetString()
                        == "Email Guest"
                );
            Assert.Equal(
                GuestContactMask.MaskEmail(seeded.GuestEmail),
                emailItem.GetProperty("guestContact").GetString()
            );
        }

        [Theory]
        [MemberData(nameof(OrdinaryRoleSurfaces))]
        public async Task OrdinarySurfaces_NeverReturnRawContact(
            string role,
            string surface
        )
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, role);
            var jwt = role == PermissionRoles.Owner
                ? seeded.OwnerJwt
                : seeded.MemberJwt;

            var response = await GetAsync(
                jwt,
                ResolveSurface(surface, seeded, seeded.InScopeLocationId)
            );
            Assert.True(
                response.StatusCode is HttpStatusCode.OK
                    or HttpStatusCode.Forbidden
                    or HttpStatusCode.NotFound,
                $"Unexpected status {(int)response.StatusCode} for {surface}"
            );
            if (role == PermissionRoles.Owner)
            {
                // Owner holds every area, so the sweep must read a body.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return;
            }

            // Search echoes the operator's own trimmed query in "q". That is
            // typed input, not stored contact, so only the echo is ignored.
            var body = (await response.Content.ReadAsStringAsync()).Replace(
                $"\"q\":\"{seeded.GuestEmail}\"",
                "\"q\":\"<query>\""
            );
            AssertNoRawContact(seeded, body);
            Assert.DoesNotContain("447700900987", body);
        }

        [Theory]
        [MemberData(nameof(OrdinarySurfacesAtForeignLocations))]
        public async Task UnassignedAndOtherTenant_Denied(
            string surface,
            string foreignLocation
        )
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );
            var locationId = foreignLocation == "OtherTenant"
                ? seeded.OtherTenantLocationId
                : seeded.OutOfScopeLocationId;

            var response = await GetAsync(
                seeded.MemberJwt,
                ResolveSurface(surface, seeded, locationId)
            );
            Assert.True(
                response.StatusCode is HttpStatusCode.Forbidden
                    or HttpStatusCode.NotFound,
                $"Expected 403 or 404 for {surface} at {foreignLocation}, "
                    + $"got {(int)response.StatusCode}"
            );
        }

        public static IEnumerable<object[]> OrdinaryRoleSurfaces()
        {
            string[] roles =
            {
                PermissionRoles.Owner,
                PermissionRoles.LocationManager,
                PermissionRoles.Marketing,
            };
            foreach (var role in roles)
            {
                foreach (var surface in OrdinarySurfaces)
                {
                    yield return new object[] { role, surface };
                }
            }
        }

        public static IEnumerable<object[]> OrdinarySurfacesAtForeignLocations()
        {
            // Feedback detail takes no locationId. It is gated by the Feedback's
            // own Location, so FeedbackDetail_ForeignLocation_Denied covers it.
            var locationSurfaces = OrdinarySurfaces
                .Where(surface => !surface.Contains("{feedbackId}"));
            foreach (var foreignLocation in new[] { "OutOfScope", "OtherTenant" })
            {
                foreach (var surface in locationSurfaces)
                {
                    yield return new object[] { surface, foreignLocation };
                }
            }
        }

        private static readonly string[] OrdinarySurfaces =
        {
            "/api/guests?locationId={locationId}",
            "/api/guests/export?locationId={locationId}",
            "/api/guests/{guestId}?locationId={locationId}",
            "/api/guests/{guestId}/feedbacks?locationId={locationId}",
            "/api/guests/{guestId}/activity?locationId={locationId}",
            "/api/feedback?locationId={locationId}",
            "/api/feedback/{feedbackId}?locationId={locationId}",
            "/api/feedback/export?locationId={locationId}&format=csv&scope=current&from={from}&to={to}",
            "/api/home/latest-activity?locationId={locationId}",
            "/api/search?locationId={locationId}&q={guestEmail}",
        };

        [Fact]
        public async Task FeedbackDetail_ForeignLocation_Denied()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );

            foreach (var locationId in new[]
            {
                seeded.OutOfScopeLocationId,
                seeded.OtherTenantLocationId,
            })
            {
                var feedbackId = SeedFeedbackAt(locationId);
                var response = await GetAsync(
                    seeded.MemberJwt,
                    $"/api/feedback/{feedbackId}?locationId={locationId}"
                );
                Assert.True(
                    response.StatusCode is HttpStatusCode.Forbidden
                        or HttpStatusCode.NotFound,
                    $"Expected 403 or 404 for Feedback at {locationId}, "
                        + $"got {(int)response.StatusCode}"
                );
            }
        }

        private int SeedFeedbackAt(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = new Feedback
            {
                RestaurantLocationId = locationId,
                GuestName = "Foreign Guest",
                GuestContact = "foreign.guest@example.com",
                ContactType = ContactType.Email,
                Comment = "Foreign location feedback",
                WorkflowStatus = FeedbackWorkflowStatus.New,
                CreatedAt = DateTime.UtcNow,
            };
            context.Feedbacks.Add(feedback);
            context.SaveChanges();
            return feedback.Id;
        }

        private static string ResolveSurface(
            string template,
            OperatorScopeFixture seeded,
            int locationId
        )
        {
            var from = Uri.EscapeDataString(
                DateTime.UtcNow.AddDays(-1).ToString("O")
            );
            var to = Uri.EscapeDataString(
                DateTime.UtcNow.AddDays(1).ToString("O")
            );
            return template
                .Replace("{locationId}", locationId.ToString())
                .Replace("{guestId}", seeded.EmailGuestId.ToString())
                .Replace("{feedbackId}", seeded.EmailFeedbackId.ToString())
                .Replace("{guestEmail}", Uri.EscapeDataString(seeded.GuestEmail))
                .Replace("{from}", from)
                .Replace("{to}", to);
        }

        private async Task<JsonElement> GetJsonAsync(string jwt, string url)
        {
            var response = await GetAsync(jwt, url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static string FeedbackExportUrl(int locationId, string extra = "")
        {
            var from = Uri.EscapeDataString(
                DateTime.UtcNow.AddDays(-1).ToString("O")
            );
            var to = Uri.EscapeDataString(
                DateTime.UtcNow.AddDays(1).ToString("O")
            );
            return $"/api/feedback/export?locationId={locationId}"
                + $"&from={from}&to={to}&scope=all-in-period&format=csv"
                + extra;
        }

        private static void AssertNoRawContact(
            OperatorScopeFixture seeded,
            string body
        )
        {
            Assert.DoesNotContain(seeded.GuestEmail, body);
            Assert.DoesNotContain(seeded.GuestMobile, body);
        }

        private async Task<string> GetCsvAsync(string jwt, string url)
        {
            var response = await GetAsync(jwt, url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        private static void AssertMaskedCsv(
            OperatorScopeFixture seeded,
            string csv
        )
        {
            Assert.DoesNotContain(seeded.GuestEmail, csv);
            Assert.DoesNotContain(seeded.GuestMobile, csv);
            Assert.Contains(
                GuestContactMask.MaskEmail(seeded.GuestEmail)!,
                csv
            );
            Assert.Contains(
                GuestContactMask.MaskPhone(seeded.GuestMobile)!,
                csv
            );
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

        private async Task<JsonElement> GetJsonAsync(string jwt, string url)
        {
            var response = await GetAsync(jwt, url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
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

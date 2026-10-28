using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>api/public/preferences</c> — session-gated preference read and writes, and the
    /// add-contact endpoints (spec §7, rulings R12).
    /// </summary>
    public class PublicGuestPreferencesEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>,
            IDisposable
    {
        private const string PreferencesPath = "/api/public/preferences";
        private const string SessionHeader = GuestPreferencesSessionToken.HeaderName;

        private readonly WebApplicationFactory<Program> _host;
        private readonly HttpClient _client;
        private readonly GuestPreferencesEndpointTestSupport.CapturingGuestPreferencesEmailService _email = new();

        public PublicGuestPreferencesEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _host = factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                    GuestPreferencesEndpointTestSupport.ReplaceSenders(services, _email)
                )
            );
            _client = _host.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            _host.Dispose();
        }

        [Fact]
        public async Task Get_WithoutSession_401()
        {
            var response = await SendAsync(HttpMethod.Get, PreferencesPath);

            await AssertSessionRejectedAsync(response);
        }

        [Fact]
        public async Task Get_WithOperatorJwt_401()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, PreferencesPath);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                new StubJwtService().GenerateToken("1", "operator@example.com", "Admin")
            );

            var response = await _client.SendAsync(request);

            await AssertSessionRejectedAsync(response);
        }

        [Fact]
        public async Task Get_ExpiredSession_401()
        {
            var session = await SeedSessionGuestAsync("prefs-expired@example.com");
            var expired = SessionTokenFor(session, DateTime.UtcNow.AddMinutes(-31));

            var response = await SendAsync(HttpMethod.Get, PreferencesPath, expired);

            await AssertSessionRejectedAsync(response);
        }

        [Fact]
        public async Task Get_SessionForMissingTummlyGuest_401()
        {
            var token = GuestPreferencesSessionToken.Create(
                999_999,
                999_999,
                DateTime.UtcNow,
                SessionSecret()
            );

            var response = await SendAsync(HttpMethod.Get, PreferencesPath, token);

            await AssertSessionRejectedAsync(response);
        }

        [Fact]
        public async Task Get_ReturnsMaskedContactNoRawEmail()
        {
            const string email = "prefs-get-masked@example.com";
            var session = await SeedSessionGuestAsync(email);

            var response = await SendAsync(
                HttpMethod.Get,
                PreferencesPath,
                SessionTokenFor(session, DateTime.UtcNow)
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var raw = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(email, raw);
            var contact = JsonDocument.Parse(raw).RootElement
                .GetProperty("contacts")
                .EnumerateArray()
                .Single();
            Assert.Equal(
                GuestPreferenceContact.Mask(TummlyGuestContactTypes.Email, email),
                contact.GetProperty("masked").GetString()
            );
        }

        [Fact]
        public async Task Get_RenewsSessionHeader()
        {
            var session = await SeedSessionGuestAsync("prefs-renew@example.com");
            // Issued 25 minutes ago: valid now, expires in 5 minutes unless renewed.
            var original = SessionTokenFor(session, DateTime.UtcNow.AddMinutes(-25));

            var response = await SendAsync(HttpMethod.Get, PreferencesPath, original);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues(SessionHeader, out var values));
            var renewed = Assert.Single(values);
            var later = DateTime.UtcNow.AddMinutes(20);
            Assert.True(
                GuestPreferencesSessionToken.TryVerify(
                    renewed,
                    SessionSecret(),
                    later,
                    out var tummlyGuestId,
                    out _
                )
            );
            Assert.Equal(session.TummlyGuestId, tummlyGuestId);
            Assert.False(
                GuestPreferencesSessionToken.TryVerify(
                    original,
                    SessionSecret(),
                    later,
                    out _,
                    out _
                )
            );
        }

        [Fact]
        public async Task PutChannels_EmailFalse_CampaignEligibilitySkipsGuest()
        {
            const string email = "prefs-channels@example.com";
            var session = await SeedSessionGuestAsync(email);
            var locationId = session.Venue.Location.Id;
            var before = await EvaluateAllEligibleAsync(locationId);
            Assert.Equal(1, before.EmailEligible);

            var response = await SendAsync(
                HttpMethod.Put,
                $"{PreferencesPath}/channels",
                SessionTokenFor(session, DateTime.UtcNow),
                new { email = false }
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var after = await EvaluateAllEligibleAsync(locationId);
            Assert.Equal(0, after.EmailEligible);
            Assert.Contains(
                after.ExcludedReasons,
                reason => reason.Reason == "withdrawn" && reason.Count == 1
            );
        }

        [Fact]
        public async Task PutName_Blank_400()
        {
            var session = await SeedSessionGuestAsync("prefs-name-blank@example.com");

            var response = await SendAsync(
                HttpMethod.Put,
                $"{PreferencesPath}/name",
                SessionTokenFor(session, DateTime.UtcNow),
                new { firstName = "   " }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await GuestPreferencesEndpointTestSupport.ReadJsonAsync(response);
            Assert.Equal("invalid", body.GetProperty("error").GetString());
        }

        [Fact]
        public async Task PutLocationMute_OtherGuestsLocation_404()
        {
            var session = await SeedSessionGuestAsync("prefs-mute-foreign@example.com");
            var other = await SeedVenueOnlyAsync("prefs-mute-other@example.com");

            var response = await SendAsync(
                HttpMethod.Put,
                $"{PreferencesPath}/locations/{other.Location.Id}/mute",
                SessionTokenFor(session, DateTime.UtcNow),
                new { muted = true }
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutLocationMute_OwnLocation_NoContent()
        {
            var session = await SeedSessionGuestAsync("prefs-mute-own@example.com");

            var response = await SendAsync(
                HttpMethod.Put,
                $"{PreferencesPath}/locations/{session.Venue.Location.Id}/mute",
                SessionTokenFor(session, DateTime.UtcNow),
                new { muted = true }
            );

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task PutRestaurantMute_OtherGuestsRestaurant_404()
        {
            var session = await SeedSessionGuestAsync("prefs-brand-mute-own@example.com");
            var other = await SeedVenueOnlyAsync("prefs-brand-mute-other@example.com");

            var response = await SendAsync(
                HttpMethod.Put,
                $"{PreferencesPath}/restaurants/{other.Restaurant.Id}/mute",
                SessionTokenFor(session, DateTime.UtcNow),
                new { muted = true }
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostContacts_SameResponseForAnyInput()
        {
            const string knownEmail = "prefs-contacts-known@example.com";
            var session = await SeedSessionGuestAsync("prefs-contacts-same@example.com");
            await SeedVenueOnlyAsync(knownEmail);
            var token = SessionTokenFor(session, DateTime.UtcNow);
            var inputs = new[]
            {
                knownEmail,
                "prefs-contacts-unknown@example.com",
                "+447911123456",
                "not-a-contact",
            };

            var bodies = new List<string>();
            foreach (var input in inputs)
            {
                var response = await SendAsync(
                    HttpMethod.Post,
                    $"{PreferencesPath}/contacts",
                    token,
                    new { contact = input }
                );

                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                bodies.Add(await response.Content.ReadAsStringAsync());
            }

            Assert.All(bodies, body => Assert.Equal(bodies[0], body));
            Assert.True(JsonDocument.Parse(bodies[0]).RootElement.GetProperty("sent").GetBoolean());
        }

        [Fact]
        public async Task PostContactsVerifyCode_WrongCode_400()
        {
            var session = await SeedSessionGuestAsync("prefs-verify-code@example.com");

            var response = await SendAsync(
                HttpMethod.Post,
                $"{PreferencesPath}/contacts/verify-code",
                SessionTokenFor(session, DateTime.UtcNow),
                new { contact = "+447911123456", code = "000000" }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await GuestPreferencesEndpointTestSupport.ReadJsonAsync(response);
            Assert.Equal("invalid", body.GetProperty("error").GetString());
        }

        [Fact]
        public async Task ContactsVerifyEmail_ReturnsLinkedTrue_NoSessionToken()
        {
            const string newEmail = "prefs-link-new@example.com";
            var owner = await SeedSessionGuestAsync("prefs-link-owner@example.com");
            using (var scope = _host.Services.CreateScope())
            {
                var verification = scope.ServiceProvider.GetRequiredService<IGuestPreferencesVerificationService>();
                await verification.StartAsync(newEmail, owner.TummlyGuestId, CancellationToken.None);
            }

            var link = (await _email.WaitForSingleAsync()).ConfirmUrl;
            Assert.Contains("?link=", link, StringComparison.Ordinal);
            var v = link[(link.IndexOf("?link=", StringComparison.Ordinal) + 6)..];

            var response = await SendAsync(
                HttpMethod.Post,
                $"{PreferencesPath}/contacts/verify-email",
                sessionToken: null,
                new { v }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains(SessionHeader));
            var body = await GuestPreferencesEndpointTestSupport.ReadJsonAsync(response);
            Assert.True(body.GetProperty("linked").GetBoolean());
            Assert.Single(body.EnumerateObject());

            using var check = _host.Services.CreateScope();
            var context = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var guest = await context.TummlyGuests
                .Include(g => g.Contacts)
                .SingleAsync(g => g.Id == owner.TummlyGuestId);
            Assert.Contains(guest.Contacts, c => c.NormalizedValue == newEmail);
        }

        [Fact]
        public async Task ContactsVerifyEmail_UnknownLink_400Invalid()
        {
            var response = await SendAsync(
                HttpMethod.Post,
                $"{PreferencesPath}/contacts/verify-email",
                sessionToken: null,
                new { v = "1.bogus" }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await GuestPreferencesEndpointTestSupport.ReadJsonAsync(response);
            Assert.Equal("invalid", body.GetProperty("error").GetString());
        }

        private static async Task AssertSessionRejectedAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains(SessionHeader));
            var body = await GuestPreferencesEndpointTestSupport.ReadJsonAsync(response);
            Assert.Equal("session", body.GetProperty("error").GetString());
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string path,
            string? sessionToken = null,
            object? body = null
        )
        {
            using var request = new HttpRequestMessage(method, path);
            if (sessionToken is not null)
            {
                request.Headers.Add(SessionHeader, sessionToken);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await _client.SendAsync(request);
        }

        private string SessionTokenFor(SeededSession session, DateTime issuedAtUtc) =>
            GuestPreferencesSessionToken.Create(
                session.TummlyGuestId,
                session.ContactId,
                issuedAtUtc,
                SessionSecret()
            );

        private string SessionSecret() =>
            GuestPreferencesEndpointTestSupport.SessionSecret(_host.Services);

        /// <summary>
        /// A verified Tummly Guest for <paramref name="email"/>, with a venue where the guest holds
        /// Email and SMS grants, so the guest is on the preference list.
        /// </summary>
        private async Task<SeededSession> SeedSessionGuestAsync(string email)
        {
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<ITummlyGuestIdentityService>();

            var proven = await identity.FindOrCreateByContactAsync(
                TummlyGuestContactTypes.Email,
                email,
                CancellationToken.None
            );
            var venue = await GuestPreferencesEndpointTestSupport.SeedVenueAsync(
                context,
                email,
                $"Prefs {email}",
                $"Prefs {email} Main"
            );

            var ledger = scope.ServiceProvider.GetRequiredService<ILocationGuestPermissionLedgerService>();
            var grantedAt = DateTime.UtcNow.AddDays(-30);
            ledger.RecordEvent(
                venue.Guest,
                venue.Location.Id,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                grantedAt
            );
            ledger.RecordEvent(
                venue.Guest,
                venue.Location.Id,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                grantedAt.AddMinutes(1)
            );
            await context.SaveChangesAsync();

            return new SeededSession(proven.TummlyGuestId, proven.ContactId, venue);
        }

        /// <summary>A venue with a Master Guest for <paramref name="email"/>, not linked to any Tummly Guest.</summary>
        private async Task<GuestPreferencesEndpointTestSupport.SeededVenue> SeedVenueOnlyAsync(string email)
        {
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await GuestPreferencesEndpointTestSupport.SeedVenueAsync(
                context,
                email,
                $"Other {email}",
                $"Other {email} Main"
            );
        }

        private async Task<CampaignEligibilityDto> EvaluateAllEligibleAsync(int locationId)
        {
            using var scope = _host.Services.CreateScope();
            var eligibility = scope.ServiceProvider.GetRequiredService<ICampaignEligibilityService>();
            return await eligibility.EvaluateAsync(locationId, "all-eligible-guests");
        }

        private sealed record SeededSession(
            int TummlyGuestId,
            int ContactId,
            GuestPreferencesEndpointTestSupport.SeededVenue Venue
        );
    }
}

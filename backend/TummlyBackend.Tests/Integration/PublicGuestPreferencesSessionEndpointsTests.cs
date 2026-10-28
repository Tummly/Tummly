using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>api/public/preferences/session/*</c> — anonymous link, start, code and email
    /// verification that returns a guest preference session token.
    /// </summary>
    public class PublicGuestPreferencesSessionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>,
            IDisposable
    {
        private const string SessionPath = "/api/public/preferences/session";

        private readonly WebApplicationFactory<Program> _host;
        private readonly HttpClient _client;
        private readonly GuestPreferencesEndpointTestSupport.CapturingGuestPreferencesEmailService _email = new();

        public PublicGuestPreferencesSessionEndpointsTests(
            TummlyWebApplicationFactory factory
        )
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
        public async Task SessionLink_ReturnsSessionToken_ForUnsubscribeToken()
        {
            const string email = "session-link-ok@example.com";
            var seeded = await SeedGuestAsync(email);
            var now = DateTime.UtcNow;
            var token = UnsubscribeToken.Create(
                seeded.LocationGuestId,
                seeded.RestaurantId,
                issuedAtUtc: now,
                secret: UnsubscribeSecret()
            );

            var response = await _client.PostAsJsonAsync(
                $"{SessionPath}/link",
                new { token }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var sessionToken = body.GetProperty("sessionToken").GetString()!;
            Assert.True(
                GuestPreferencesSessionToken.TryVerify(
                    sessionToken,
                    SessionSecret(),
                    now,
                    out var tummlyGuestId,
                    out var contactId
                )
            );
            Assert.True(tummlyGuestId > 0);
            Assert.True(contactId > 0);
        }

        [Fact]
        public async Task SessionLink_Invalid_ReturnsValidFalse()
        {
            const string email = "session-link-bad@example.com";
            var seeded = await SeedGuestAsync(email);
            var token = UnsubscribeToken.Create(
                seeded.LocationGuestId,
                seeded.RestaurantId,
                issuedAtUtc: DateTime.UtcNow,
                secret: UnsubscribeSecret()
            );
            var parts = token.Split('.');
            var tampered = parts[0] + "x." + parts[1];

            var response = await _client.PostAsJsonAsync(
                $"{SessionPath}/link",
                new { token = tampered }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("valid").GetBoolean());
            Assert.False(body.TryGetProperty("sessionToken", out _));
        }

        [Fact]
        public async Task SessionStart_UnknownAndKnown_SameResponse()
        {
            const string known = "session-start-known@example.com";
            await SeedGuestAsync(known);

            var unknownResponse = await _client.PostAsJsonAsync(
                $"{SessionPath}/start",
                new { contact = "session-start-unknown@example.com" }
            );
            var knownResponse = await _client.PostAsJsonAsync(
                $"{SessionPath}/start",
                new { contact = known }
            );

            Assert.Equal(HttpStatusCode.Accepted, unknownResponse.StatusCode);
            Assert.Equal(unknownResponse.StatusCode, knownResponse.StatusCode);
            var unknownBody = await unknownResponse.Content.ReadAsStringAsync();
            var knownBody = await knownResponse.Content.ReadAsStringAsync();
            Assert.Equal(unknownBody, knownBody);
            Assert.True(JsonDocument.Parse(knownBody).RootElement.GetProperty("sent").GetBoolean());

            var sent = (await WaitForSingleEmailAsync());
            Assert.Equal(known, sent.ToEmail);
        }

        [Fact]
        public async Task VerifyEmail_ThenToken_Verifies()
        {
            const string email = "session-verify-email@example.com";
            await SeedGuestAsync(email);
            var started = await _client.PostAsJsonAsync(
                $"{SessionPath}/start",
                new { contact = email }
            );
            Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
            var link = (await WaitForSingleEmailAsync()).ConfirmUrl;
            var v = link[(link.IndexOf("?v=", StringComparison.Ordinal) + 3)..];
            var now = DateTime.UtcNow;

            var response = await _client.PostAsJsonAsync(
                $"{SessionPath}/verify-email",
                new { v }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var sessionToken = body.GetProperty("sessionToken").GetString()!;
            Assert.True(
                GuestPreferencesSessionToken.TryVerify(
                    sessionToken,
                    SessionSecret(),
                    now,
                    out _,
                    out _
                )
            );
        }

        [Fact]
        public async Task VerifyEmail_Reused_Returns400Invalid()
        {
            const string email = "session-verify-reuse@example.com";
            await SeedGuestAsync(email);
            await _client.PostAsJsonAsync($"{SessionPath}/start", new { contact = email });
            var link = (await WaitForSingleEmailAsync()).ConfirmUrl;
            var v = link[(link.IndexOf("?v=", StringComparison.Ordinal) + 3)..];
            await _client.PostAsJsonAsync($"{SessionPath}/verify-email", new { v });

            var response = await _client.PostAsJsonAsync(
                $"{SessionPath}/verify-email",
                new { v }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("invalid", body.GetProperty("error").GetString());
            Assert.False(body.TryGetProperty("sessionToken", out _));
        }

        [Fact]
        public async Task VerifyCode_WrongCode_Returns400Invalid()
        {
            var response = await _client.PostAsJsonAsync(
                $"{SessionPath}/verify-code",
                new { contact = "+447911123456", code = "000000" }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("invalid", body.GetProperty("error").GetString());
        }

        [Fact]
        public async Task SessionVerifyEmail_AddContactLink_Invalid_NotConsumed()
        {
            const string ownerEmail = "addcontact-owner@example.com";
            const string newEmail = "addcontact-new@example.com";
            int ownerId;

            using (var scope = _host.Services.CreateScope())
            {
                var identity = scope.ServiceProvider.GetRequiredService<ITummlyGuestIdentityService>();
                var owner = await identity.FindOrCreateByContactAsync(
                    TummlyGuestContactTypes.Email,
                    ownerEmail,
                    CancellationToken.None
                );
                ownerId = owner.TummlyGuestId;

                var verification = scope.ServiceProvider.GetRequiredService<IGuestPreferencesVerificationService>();
                await verification.StartAsync(newEmail, ownerId, CancellationToken.None);
            }

            var link = (await WaitForSingleEmailAsync()).ConfirmUrl;
            Assert.Contains("?link=", link, StringComparison.Ordinal);
            var v = link[(link.IndexOf("?link=", StringComparison.Ordinal) + 6)..];

            var response = await _client.PostAsJsonAsync(
                $"{SessionPath}/verify-email",
                new { v }
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("invalid", body.GetProperty("error").GetString());
            Assert.False(body.TryGetProperty("sessionToken", out _));

            using var check = _host.Services.CreateScope();
            var add = check.ServiceProvider.GetRequiredService<IGuestPreferencesVerificationService>();
            var result = await add.VerifyAddContactEmailLinkAsync(v, CancellationToken.None);

            Assert.Equal(GuestVerifyOutcome.Ok, result.Outcome);
            Assert.Equal(ownerId, result.Proven!.TummlyGuestId);
        }

        private Task<(string ToEmail, string ConfirmUrl)> WaitForSingleEmailAsync() =>
            _email.WaitForSingleAsync();

        private string UnsubscribeSecret() =>
            GuestPreferencesEndpointTestSupport.UnsubscribeSecret(_host.Services);

        private string SessionSecret() =>
            GuestPreferencesEndpointTestSupport.SessionSecret(_host.Services);

        private async Task<SeededGuest> SeedGuestAsync(string email)
        {
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var venue = await GuestPreferencesEndpointTestSupport.SeedVenueAsync(
                context,
                email,
                $"Session {email}",
                $"Session {email} Main"
            );

            return new SeededGuest(venue.Restaurant.Id, venue.Guest.Id);
        }

        private static Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
            GuestPreferencesEndpointTestSupport.ReadJsonAsync(response);

        private sealed record SeededGuest(int RestaurantId, int LocationGuestId);
    }
}

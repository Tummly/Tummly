using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.TrustIndicators;

namespace TummlyBackend.Tests.Integration
{
    public sealed class AdminTrustIndicatorsEndpointsTests : IDisposable
    {
        private readonly TummlyWebApplicationFactory _factory = new();
        private readonly HttpClient _client;

        public AdminTrustIndicatorsEndpointsTests()
        {
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Fact]
        public async Task Get_WithoutToken_Returns401()
        {
            using var response = await _client.GetAsync("/api/admin/trust-indicators");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_AsSupport_Returns403()
        {
            var admins = await SeedAdminsAsync();

            using var response = await SendAsync(
                HttpMethod.Get,
                "/api/admin/trust-indicators",
                admins.SupportJwt
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Get_AsAdmin_ReturnsFifteenDraftItems()
        {
            var admins = await SeedAdminsAsync();

            using var response = await SendAsync(
                HttpMethod.Get,
                "/api/admin/trust-indicators",
                admins.EditorJwt
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var draft = body.GetProperty("item").GetProperty("draft");
            Assert.Equal(15, draft.GetArrayLength());
            Assert.Equal("ICO Data Protection Registration", draft[0].GetProperty("title").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("item").GetProperty("published").ValueKind);
        }

        [Fact]
        public async Task Public_WhenNothingIsPublished_ReturnsEmptyArray()
        {
            await SeedAdminsAsync();

            using var response = await _client.GetAsync("/api/public/trust-indicators");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, body.ValueKind);
            Assert.Equal(0, body.GetArrayLength());
        }

        [Fact]
        public async Task Flow_EditSubmitPublish_OverHttp_EnforcesFourEyes_AndServesPublicRead()
        {
            var admins = await SeedAdminsAsync();
            var proof = "https://ico.example/proof";

            using var edit = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/1",
                admins.EditorJwt,
                new
                {
                    enabled = true,
                    status = "VerifiedPublicRecord",
                    statusLabel = "Registered",
                    ctaType = "ExternalProof",
                    ctaTarget = proof,
                    ctaLabel = "View registration",
                    evidenceUrl = proof,
                    issuer = "Information Commissioner’s Office",
                    altText = string.Empty,
                    tooltip = string.Empty,
                }
            );
            Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

            using var submit = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/submit",
                admins.EditorJwt
            );
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            using var selfPublish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.EditorJwt,
                new { reason = "Evidence checked" },
                key: "http-self"
            );
            Assert.Equal(HttpStatusCode.Conflict, selfPublish.StatusCode);
            var denied = await selfPublish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("self_approval_denied", denied.GetProperty("code").GetString());

            using var noKey = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                new { reason = "Evidence checked" }
            );
            Assert.Equal(HttpStatusCode.BadRequest, noKey.StatusCode);
            var missingKey = await noKey.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("idempotency_key_required", missingKey.GetProperty("code").GetString());

            using var publish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                new { reason = "Evidence checked" },
                key: "http-publish"
            );
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
            var published = await publish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, published.GetProperty("item").GetProperty("version").GetInt32());

            using var repeat = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                new { reason = "Evidence checked" },
                key: "http-publish"
            );
            Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
            var replayed = await repeat.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, replayed.GetProperty("item").GetProperty("version").GetInt32());

            using var publicRead = await _client.GetAsync("/api/public/trust-indicators");
            Assert.Equal(HttpStatusCode.OK, publicRead.StatusCode);
            var live = await publicRead.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, live.GetArrayLength());
            Assert.Equal(1, live[0].GetProperty("itemNumber").GetInt32());
            Assert.Equal(proof, live[0].GetProperty("ctaTarget").GetString());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await context.TrustIndicatorPublications.CountAsync());
            Assert.Equal(
                1,
                await context.AdminAuditEvents.CountAsync(e =>
                    e.Action == AdminAuditActions.TrustIndicatorPublish
                )
            );
            Assert.Equal(
                1,
                await context.AdminAuditEvents.CountAsync(e =>
                    e.Action == AdminAuditActions.TrustIndicatorRefused
                )
            );
        }

        private async Task<(string EditorJwt, string PublisherJwt, string SupportJwt)> SeedAdminsAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            await TrustIndicatorCatalogSeed.EnsureSeededAsync(context);

            var editor = new Admin
            {
                FullName = "Trust Editor",
                Email = $"editor-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            var publisher = new Admin
            {
                FullName = "Trust Publisher",
                Email = $"publisher-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            var support = new Admin
            {
                FullName = "Trust Support",
                Email = $"support-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Support",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Admins.AddRange(editor, publisher, support);
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateAdminToken(editor),
                jwtService.GenerateAdminToken(publisher),
                jwtService.GenerateAdminToken(support)
            );
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string url,
            string jwt,
            object? body = null,
            string? key = null
        )
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            if (key != null)
            {
                request.Headers.Add("Idempotency-Key", key);
            }

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await _client.SendAsync(request);
        }
    }
}

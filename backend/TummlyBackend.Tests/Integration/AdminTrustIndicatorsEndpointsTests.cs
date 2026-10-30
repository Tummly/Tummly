using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Controllers;
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
        public async Task Support_IsForbidden_OnEveryTrustEndpoint()
        {
            var admins = await SeedAdminsAsync();
            var jwt = admins.SupportJwt;

            using var get = await SendAsync(HttpMethod.Get, "/api/admin/trust-indicators", jwt);
            using var put = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/1",
                jwt,
                new { status = "Roadmap", statusLabel = "On our roadmap" }
            );
            using var submit = await SendAsync(HttpMethod.Post, "/api/admin/trust-indicators/submit", jwt);
            using var publish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                jwt,
                new { reason = "Evidence checked" },
                key: "support-key"
            );

            Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, publish.StatusCode);
        }

        [Fact]
        public async Task Anonymous_IsUnauthorized_OnEveryTrustEndpoint()
        {
            await SeedAdminsAsync();

            using var get = await _client.GetAsync("/api/admin/trust-indicators");
            using var put = await _client.PutAsJsonAsync(
                "/api/admin/trust-indicators/items/1",
                new { status = "Roadmap" }
            );
            using var submit = await _client.PostAsync("/api/admin/trust-indicators/submit", null);
            using var publish = await _client.PostAsJsonAsync(
                "/api/admin/trust-indicators/publish",
                new { reason = "Evidence checked" }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, submit.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, publish.StatusCode);
        }

        [Fact]
        public async Task Get_ReportsViewerCanApprove_PerActor()
        {
            var admins = await SeedAdminsAsync();

            var approver = await GetViewerAsync(admins.PublisherJwt);
            var nonApprover = await GetViewerAsync(admins.NonApproverJwt);

            Assert.True(approver.GetProperty("canApprove").GetBoolean());
            Assert.False(nonApprover.GetProperty("canApprove").GetBoolean());
        }

        [Fact]
        public async Task NonApproverAdmin_PublishIsForbidden_WithApproverRequired_AndAudited()
        {
            var admins = await SeedAdminsAsync();
            using (var edit = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/1",
                admins.EditorJwt,
                new
                {
                    enabled = true,
                    status = "VerifiedPublicRecord",
                    statusLabel = "Registered",
                    ctaType = "ExternalProof",
                    ctaTarget = "https://ico.example/proof",
                    ctaLabel = "View registration",
                    evidenceUrl = "https://ico.example/proof",
                    issuer = "Information Commissioner’s Office",
                }
            ))
            {
                Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
            }
            using (var submit = await SendAsync(HttpMethod.Post, "/api/admin/trust-indicators/submit", admins.EditorJwt))
            {
                Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
            }

            using var publish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.NonApproverJwt,
                new { reason = "Evidence checked" },
                key: "http-non-approver"
            );

            Assert.Equal(HttpStatusCode.Forbidden, publish.StatusCode);
            var body = await publish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("approver_required", body.GetProperty("code").GetString());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await context.TrustIndicatorPublications.CountAsync());
            Assert.Equal(
                1,
                await context.AdminAuditEvents.CountAsync(e =>
                    e.Action == AdminAuditActions.TrustIndicatorRefused
                )
            );
        }

        [Fact]
        public void No_Controller_Action_Reaches_Admin_Or_CanApproveTrustIndicators()
        {
            // No endpoint may set the approver flag. Walk every action parameter type and
            // all of its public properties, recursively, and fail on the Admin entity or on
            // any type that carries CanApproveTrustIndicators.
            var roots = typeof(AdminTrustIndicatorsController).Assembly
                .GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
                .SelectMany(type => type.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .SelectMany(method => method.GetParameters())
                .Select(parameter => parameter.ParameterType)
                .ToList();

            var offenders = new List<string>();
            var visited = new HashSet<Type>();
            foreach (var root in roots)
            {
                WalkForApproverFlag(root, visited, offenders);
            }

            Assert.Empty(offenders);

            // The walker must detect the entity it guards, or the empty result above proves nothing.
            var detected = new List<string>();
            WalkForApproverFlag(typeof(Admin), new HashSet<Type>(), detected);
            Assert.Contains(typeof(Admin).FullName!, detected);
        }

        /// <summary>
        /// Visits a type, its generic arguments, array element and the types of its public
        /// properties. Only TummlyBackend types are expanded; framework and primitive types are skipped.
        /// </summary>
        private static void WalkForApproverFlag(Type type, HashSet<Type> visited, List<string> offenders)
        {
            var appAssembly = typeof(Admin).Assembly;
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying.IsArray)
            {
                WalkForApproverFlag(underlying.GetElementType()!, visited, offenders);
                return;
            }

            if (underlying.IsGenericType)
            {
                foreach (var argument in underlying.GetGenericArguments())
                {
                    WalkForApproverFlag(argument, visited, offenders);
                }
            }

            if (underlying.IsPrimitive
                || underlying.IsEnum
                || underlying == typeof(string)
                || underlying.Assembly != appAssembly
                || !visited.Add(underlying))
            {
                return;
            }

            if (underlying == typeof(Admin)
                || underlying.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(property => property.Name == nameof(Admin.CanApproveTrustIndicators)))
            {
                offenders.Add(underlying.FullName!);
            }

            foreach (var property in underlying.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                WalkForApproverFlag(property.PropertyType, visited, offenders);
            }
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

            var revision = await SubmitAsync(admins.EditorJwt);

            using var selfPublish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.EditorJwt,
                PublishBody(revision),
                key: "http-self"
            );
            Assert.Equal(HttpStatusCode.Conflict, selfPublish.StatusCode);
            var denied = await selfPublish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("self_approval_denied", denied.GetProperty("code").GetString());

            using var noKey = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                PublishBody(revision)
            );
            Assert.Equal(HttpStatusCode.BadRequest, noKey.StatusCode);
            var missingKey = await noKey.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("idempotency_key_required", missingKey.GetProperty("code").GetString());

            using var publish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                PublishBody(revision),
                key: "http-publish"
            );
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
            var published = await publish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, published.GetProperty("item").GetProperty("version").GetInt32());

            using var repeat = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                PublishBody(revision),
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
            Assert.Equal("Verified", live[0].GetProperty("status").GetString());
            Assert.Equal(proof, live[0].GetProperty("cta").GetProperty("href").GetString());

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

        [Fact]
        public async Task ReplayOfAnotherAdminsKey_Returns409IdempotencyKeyReused()
        {
            var admins = await SeedAdminsAsync();
            await PublishItemOneAsync(admins, "http-replay-other", enabled: true);

            using var replay = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.EditorJwt,
                new { reason = "Evidence checked" },
                key: "http-replay-other"
            );

            Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
            var body = await replay.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("idempotency_key_reused", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task PublishWithStaleRevision_Returns409DraftChanged()
        {
            var admins = await SeedAdminsAsync();
            using (var edit = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/1",
                admins.EditorJwt,
                ItemOneBody(enabled: true, link: "https://ico.example/proof")
            ))
            {
                Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
            }
            var reviewed = await SubmitAsync(admins.EditorJwt);
            using (var edit = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/2",
                admins.EditorJwt,
                new
                {
                    enabled = false,
                    status = "VerifiedCredential",
                    statusLabel = "Self-assessment completed",
                    ctaType = "None",
                    tooltip = "Changed after the review",
                }
            ))
            {
                Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
            }
            await SubmitAsync(admins.EditorJwt);

            using var publish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                PublishBody(reviewed),
                key: "http-stale-revision"
            );

            Assert.Equal(HttpStatusCode.Conflict, publish.StatusCode);
            var body = await publish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("draft_changed", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Public_HasNoStoreHeader()
        {
            using var response = await _client.GetAsync("/api/public/trust-indicators");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Cache-Control", out var values));
            Assert.Equal("no-store", Assert.Single(values));
        }

        [Fact]
        public async Task PublishedOffItemDisappears()
        {
            var admins = await SeedAdminsAsync();

            await PublishItemOneAsync(admins, "http-item1-on", enabled: true);
            var on = await _client.GetFromJsonAsync<JsonElement>("/api/public/trust-indicators");
            Assert.Equal(1, on.GetArrayLength());
            Assert.Equal(1, on[0].GetProperty("itemNumber").GetInt32());

            await PublishItemOneAsync(admins, "http-item1-off", enabled: false);
            var off = await _client.GetFromJsonAsync<JsonElement>("/api/public/trust-indicators");
            Assert.Equal(0, off.GetArrayLength());
        }

        [Fact]
        public async Task UnpublishAllGivesEmptyArray()
        {
            var admins = await SeedAdminsAsync();
            await PublishItemOneAsync(admins, "http-unpublish-on", enabled: true);

            await PublishItemOneAsync(admins, "http-unpublish-off", enabled: false);

            using var response = await _client.GetAsync("/api/public/trust-indicators");
            Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FailedPublishKeepsPreviousPublicOutput()
        {
            var admins = await SeedAdminsAsync();
            await PublishItemOneAsync(admins, "http-v1", enabled: true);
            var before = await ReadPublicBodyAsync();

            // Rule gate: item 03 is a roadmap item and may not carry a call to action.
            using (var edit = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/3",
                admins.EditorJwt,
                new
                {
                    enabled = true,
                    status = "Roadmap",
                    statusLabel = "On our roadmap",
                    ctaType = "InternalPage",
                    ctaTarget = "/security",
                    ctaLabel = "Read more",
                    altText = string.Empty,
                    tooltip = string.Empty,
                }
            ))
            {
                Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
            }
            var gateRevision = await SubmitAsync(admins.EditorJwt);
            using (var gate = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                PublishBody(gateRevision),
                key: "http-gate-fail"
            ))
            {
                Assert.Equal(HttpStatusCode.BadRequest, gate.StatusCode);
                var body = await gate.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("publish_gate_failed", body.GetProperty("code").GetString());
            }
            Assert.Equal(before, await ReadPublicBodyAsync());

            // Put item 03 back to disabled so the next refusal can only come from the link check.
            using (var revert = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/3",
                admins.EditorJwt,
                new { enabled = false, status = "Roadmap", statusLabel = "On our roadmap", ctaType = "None" }
            ))
            {
                Assert.Equal(HttpStatusCode.OK, revert.StatusCode);
            }

            // Link check: item 01 now points at a link that answers 404.
            const string deadLink = "https://ico.example/item1-dead";
            _factory.EvidenceLinks.Results[deadLink] = new TrustEvidenceLinkResult(false, "HTTP 404");
            using (var edit = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/1",
                admins.EditorJwt,
                ItemOneBody(enabled: true, link: deadLink)
            ))
            {
                Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
            }
            var linkRevision = await SubmitAsync(admins.EditorJwt);
            using (var linkFail = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                PublishBody(linkRevision),
                key: "http-link-fail"
            ))
            {
                Assert.Equal(HttpStatusCode.BadRequest, linkFail.StatusCode);
                var body = await linkFail.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("publish_gate_failed", body.GetProperty("code").GetString());
                Assert.Contains(
                    "Item 01: evidence link failed (HTTP 404).",
                    body.GetProperty("details").EnumerateArray().Select(d => d.GetString())
                );
            }
            Assert.Equal(before, await ReadPublicBodyAsync());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await context.TrustIndicatorPublications.CountAsync());
            Assert.Null(await context.TrustIndicatorPublications.FirstOrDefaultAsync(p => p.Version == 2));
        }

        private async Task<string> ReadPublicBodyAsync()
        {
            using var response = await _client.GetAsync("/api/public/trust-indicators");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>Edits item 01, submits as the editor and publishes as the publisher, all over HTTP.</summary>
        private async Task PublishItemOneAsync(
            (string EditorJwt, string PublisherJwt, string SupportJwt, string NonApproverJwt) admins,
            string key,
            bool enabled
        )
        {
            using (var edit = await SendAsync(
                HttpMethod.Put,
                "/api/admin/trust-indicators/items/1",
                admins.EditorJwt,
                ItemOneBody(enabled, "https://ico.example/proof")
            ))
            {
                Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
            }
            var revision = await SubmitAsync(admins.EditorJwt);
            using var publish = await SendAsync(
                HttpMethod.Post,
                "/api/admin/trust-indicators/publish",
                admins.PublisherJwt,
                PublishBody(revision),
                key: key
            );
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        }

        /// <summary>Submits the draft as the editor and returns the revision the submit recorded.</summary>
        private async Task<string> SubmitAsync(string editorJwt)
        {
            using var submit = await SendAsync(HttpMethod.Post, "/api/admin/trust-indicators/submit", editorJwt);
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
            var body = await submit.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("item").GetProperty("draftRevision").GetString()!;
        }

        private static object PublishBody(string draftRevision) =>
            new { reason = "Evidence checked", draftRevision };

        private static object ItemOneBody(bool enabled, string link) =>
            new
            {
                enabled,
                status = "VerifiedPublicRecord",
                statusLabel = "Registered",
                ctaType = enabled ? "ExternalProof" : "None",
                ctaTarget = enabled ? link : null,
                ctaLabel = enabled ? "View registration" : null,
                evidenceUrl = link,
                issuer = "Information Commissioner’s Office",
                altText = string.Empty,
                tooltip = string.Empty,
            };

        private async Task<JsonElement> GetViewerAsync(string jwt)
        {
            using var response = await SendAsync(HttpMethod.Get, "/api/admin/trust-indicators", jwt);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("item").GetProperty("viewer");
        }

        private async Task<(string EditorJwt, string PublisherJwt, string SupportJwt, string NonApproverJwt)> SeedAdminsAsync()
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
                CanApproveTrustIndicators = true,
                CreatedAt = DateTime.UtcNow,
            };
            var publisher = new Admin
            {
                FullName = "Trust Publisher",
                Email = $"publisher-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CanApproveTrustIndicators = true,
                CreatedAt = DateTime.UtcNow,
            };
            var nonApprover = new Admin
            {
                FullName = "Trust Reader",
                Email = $"reader-{Guid.NewGuid():N}@tummly.com",
                PasswordHash = "hash",
                Role = "Admin",
                IsActive = true,
                CanApproveTrustIndicators = false,
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
            context.Admins.AddRange(editor, publisher, support, nonApprover);
            await context.SaveChangesAsync();

            return (
                jwtService.GenerateAdminToken(editor),
                jwtService.GenerateAdminToken(publisher),
                jwtService.GenerateAdminToken(support),
                jwtService.GenerateAdminToken(nonApprover)
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

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class OffersCatalogEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersCatalogEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostOffer_CreatesActiveCatalogDefinition()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-catalog-create");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "percentage_discount",
                    title = "10% off next visit",
                    description = "Enjoy 10% off your next meal with us.",
                    validity = "30_days_after_issue",
                    discountPercentage = 10m,
                    staffInstructions = "Ask for the unique code.",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var offer = body.GetProperty("offer");
            Assert.True(offer.GetProperty("id").GetInt32() > 0);
            Assert.Equal(seeded.LocationId, offer.GetProperty("locationId").GetInt32());
            Assert.Equal("draft", offer.GetProperty("status").GetString());
            Assert.Equal("percentage_discount", offer.GetProperty("offerType").GetString());
            Assert.Equal("10% off next visit", offer.GetProperty("title").GetString());
            Assert.Equal(10m, offer.GetProperty("discountPercentage").GetDecimal());
            Assert.False(offer.TryGetProperty("redemptionCode", out _));
        }

        [Fact]
        public async Task PostOfferDraft_CreatesStoredDraft_Attachable()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-catalog-draft-create");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/draft",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "percentage_discount",
                    title = "Draft 10% off",
                    description = "Stored Draft — attachable; Active after first live attach.",
                    validity = "7_days_after_issue",
                    discountPercentage = 10m,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var offer = body.GetProperty("offer");
            var offerId = offer.GetProperty("id").GetInt32();
            Assert.True(offerId > 0);
            Assert.Equal(seeded.LocationId, offer.GetProperty("locationId").GetInt32());
            Assert.Equal("draft", offer.GetProperty("status").GetString());
            Assert.Equal("Draft 10% off", offer.GetProperty("title").GetString());

            using var scope = _factory.Services.CreateScope();
            var offers = scope.ServiceProvider
                .GetRequiredService<IOffersCatalogService>();
            Assert.True(
                await offers.IsAttachableForLocationAsync(offerId, seeded.LocationId)
            );

            using var listRequest = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=drafts&page=1&pageSize=25&utcOffsetMinutes=0",
                seeded.Jwt
            );
            var listResponse = await _client.SendAsync(listRequest);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listBody = await ReadJsonAsync(listResponse);
            var items = listBody.GetProperty("items");
            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal(offerId, items[0].GetProperty("id").GetInt32());
            Assert.Equal("draft", items[0].GetProperty("status").GetString());
            Assert.True(listBody.GetProperty("tabCounts").GetProperty("drafts").GetInt32() >= 1);
        }

        [Fact]
        public async Task PostCampaign_AttachesOfferId_WithCreateNewOfferStance()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-attach-offer");
            var offerId = await CreateOfferAsync(seeded, "Attach me");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Thank with offer",
                    goalId = "thank-recent-guests",
                    audienceKey = "all-eligible-guests",
                    channel = "email",
                    offerStance = "create-new-offer",
                    offerId,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var campaign = (await ReadJsonAsync(response)).GetProperty("campaign");
            Assert.Equal("create-new-offer", campaign.GetProperty("offerStance").GetString());
            Assert.Equal(offerId, campaign.GetProperty("offerId").GetInt32());
        }

        [Fact]
        public async Task PatchCampaign_NoOffer_ClearsOfferId()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-clear-offer");
            var offerId = await CreateOfferAsync(seeded, "Clear me");

            using var createRequest = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Clear offer draft",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId,
                }
            );
            var createResponse = await _client.SendAsync(createRequest);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            var created = (await ReadJsonAsync(createResponse)).GetProperty("campaign");
            var id = created.GetProperty("id").GetInt32();
            var rowVersion = created.GetProperty("rowVersion").GetString();

            using var patchRequest = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new { rowVersion, offerStance = "no-offer" }
            );
            var patchResponse = await _client.SendAsync(patchRequest);
            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            var campaign = (await ReadJsonAsync(patchResponse)).GetProperty("campaign");
            Assert.Equal("no-offer", campaign.GetProperty("offerStance").GetString());
            Assert.Equal(JsonValueKind.Null, campaign.GetProperty("offerId").ValueKind);
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenOfferIdIsOrphan()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-orphan-offer");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Orphan offer",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId = 999_999,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Contains(
                "offerId",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenOfferIsExpiredByFixedDate()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-expired-offer");
            var offerId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Expired attach",
                status: "active",
                validity: CatalogOfferValidity.ChooseExpiryDate,
                customExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Attach expired",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetOffers_List_FiltersTabsSearchAndStatus()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-list-tabs");
            var draftLikeId = await CreateOfferAsync(seeded, "Alpha draft-like");
            var inFlightId = await CreateOfferAsync(seeded, "Bravo attached");
            var pausedId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Charlie paused",
                status: "paused"
            );
            var needsAttentionId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Delta soon",
                status: "active",
                validity: CatalogOfferValidity.ChooseExpiryDate,
                customExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))
            );

            using var attachRequest = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Winter campaign",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId = inFlightId,
                }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(attachRequest)).StatusCode
            );

            using var attachNeedsAttention = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Expiring campaign",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId = needsAttentionId,
                }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(attachNeedsAttention)).StatusCode
            );

            using var listAll = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=all&page=1&pageSize=25&utcOffsetMinutes=0",
                seeded.Jwt
            );
            var allBody = await ReadJsonAsync(await _client.SendAsync(listAll));
            Assert.True(allBody.GetProperty("success").GetBoolean());
            Assert.Equal(4, allBody.GetProperty("totalCount").GetInt32());
            var tabs = allBody.GetProperty("tabCounts");
            Assert.Equal(4, tabs.GetProperty("all").GetInt32());
            Assert.Equal(1, tabs.GetProperty("drafts").GetInt32());
            Assert.Equal(2, tabs.GetProperty("inFlight").GetInt32());
            Assert.Equal(1, tabs.GetProperty("sent").GetInt32());
            Assert.Equal(1, tabs.GetProperty("needsAttention").GetInt32());

            using var drafts = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=drafts&page=1&pageSize=25",
                seeded.Jwt
            );
            var draftsBody = await ReadJsonAsync(await _client.SendAsync(drafts));
            var draftIds = draftsBody.GetProperty("items")
                .EnumerateArray()
                .Select(item => item.GetProperty("id").GetInt32())
                .ToHashSet();
            Assert.Contains(draftLikeId, draftIds);
            Assert.DoesNotContain(needsAttentionId, draftIds);
            Assert.DoesNotContain(inFlightId, draftIds);
            Assert.DoesNotContain(pausedId, draftIds);

            using var search = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=all&q=Winter&page=1&pageSize=25",
                seeded.Jwt
            );
            var searchBody = await ReadJsonAsync(await _client.SendAsync(search));
            Assert.Equal(1, searchBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                inFlightId,
                searchBody.GetProperty("items")[0].GetProperty("id").GetInt32()
            );
            Assert.Contains(
                "campaign",
                searchBody.GetProperty("items")[0]
                    .GetProperty("attachKinds")
                    .EnumerateArray()
                    .Select(v => v.GetString())
            );

            using var statusFilter = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=all&status=paused&page=1&pageSize=25",
                seeded.Jwt
            );
            var statusBody = await ReadJsonAsync(
                await _client.SendAsync(statusFilter)
            );
            Assert.Equal(1, statusBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "paused",
                statusBody.GetProperty("items")[0].GetProperty("status").GetString()
            );

            using var attachFilter = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=all&attachSource=recovery&page=1&pageSize=25",
                seeded.Jwt
            );
            var attachBody = await ReadJsonAsync(
                await _client.SendAsync(attachFilter)
            );
            Assert.Equal(0, attachBody.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetOffers_ExistingOfferPickerQuery_AllowsPageSize100()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-picker-pagesize");
            var offerId = await CreateOfferAsync(seeded, "Picker offer");

            using var list = AuthorizedGet(
                $"/api/offers?locationId={seeded.LocationId}&view=all&sort=recent-activity&page=1&pageSize=100&utcOffsetMinutes=300&status=draft&status=active",
                seeded.Jwt
            );
            var response = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(100, body.GetProperty("pageSize").GetInt32());
            Assert.Equal(
                offerId,
                body.GetProperty("items")[0].GetProperty("id").GetInt32()
            );
        }

        [Fact]
        public async Task Lifecycle_PauseResumeArchiveDuplicate()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-lifecycle");
            var offerId = await CreateOfferAsync(seeded, "Lifecycle offer");

            using var attach = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Lifecycle campaign",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId,
                }
            );
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(attach)).StatusCode);

            using var pause = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/pause",
                seeded.Jwt,
                new { }
            );
            var pauseResponse = await _client.SendAsync(pause);
            Assert.Equal(HttpStatusCode.OK, pauseResponse.StatusCode);
            Assert.Equal(
                "paused",
                (await ReadJsonAsync(pauseResponse))
                    .GetProperty("offer")
                    .GetProperty("status")
                    .GetString()
            );

            using var pauseAgain = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/pause",
                seeded.Jwt,
                new { }
            );
            var pauseAgainResponse = await _client.SendAsync(pauseAgain);
            Assert.Equal(HttpStatusCode.Conflict, pauseAgainResponse.StatusCode);
            Assert.Equal(
                "invalid_status",
                (await ReadJsonAsync(pauseAgainResponse)).GetProperty("code").GetString()
            );

            using var resume = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/resume",
                seeded.Jwt,
                new { }
            );
            var resumeResponse = await _client.SendAsync(resume);
            Assert.Equal(HttpStatusCode.OK, resumeResponse.StatusCode);
            Assert.Equal(
                "active",
                (await ReadJsonAsync(resumeResponse))
                    .GetProperty("offer")
                    .GetProperty("status")
                    .GetString()
            );

            using var archive = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/archive",
                seeded.Jwt,
                new { }
            );
            var archiveResponse = await _client.SendAsync(archive);
            Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
            Assert.Equal(
                "archived",
                (await ReadJsonAsync(archiveResponse))
                    .GetProperty("offer")
                    .GetProperty("status")
                    .GetString()
            );

            using var duplicate = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/duplicate",
                seeded.Jwt,
                new { }
            );
            var duplicateResponse = await _client.SendAsync(duplicate);
            Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
            var copy = (await ReadJsonAsync(duplicateResponse)).GetProperty("offer");
            Assert.True(copy.GetProperty("id").GetInt32() != offerId);
            Assert.Equal("draft", copy.GetProperty("status").GetString());
            Assert.Equal(
                "Lifecycle offer (copy)",
                copy.GetProperty("title").GetString()
            );
        }

        [Fact]
        public async Task GetOffer_ReturnsEffectiveExpiredStatus()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-get-expired");
            var offerId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Past expiry",
                status: "active",
                validity: CatalogOfferValidity.ChooseExpiryDate,
                customExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            );

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}?utcOffsetMinutes=0",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "expired",
                (await ReadJsonAsync(response))
                    .GetProperty("offer")
                    .GetProperty("status")
                    .GetString()
            );
        }

        [Fact]
        public async Task PutOffer_UpdatesEditableFields_AndReturnsIssueCount()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-catalog-update");
            var offerId = await CreateOfferAsync(seeded, "Before edit");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId);
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                "TUM-111111",
                discountAmount: 5m
            );

            using var getBefore = AuthorizedGet(
                $"/api/offers/{offerId}",
                seeded.Jwt
            );
            var beforeBody = await ReadJsonAsync(await _client.SendAsync(getBefore));
            Assert.Equal(1, beforeBody.GetProperty("offer").GetProperty("issueCount").GetInt32());

            using var request = AuthorizedJson(
                HttpMethod.Put,
                $"/api/offers/{offerId}",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "fixed_discount",
                    title = "After edit",
                    description = "Updated description for new issues.",
                    validity = "30_days_after_issue",
                    discountAmount = 8m,
                    staffInstructions = "New staff note.",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var offer = (await ReadJsonAsync(response)).GetProperty("offer");
            Assert.Equal(offerId, offer.GetProperty("id").GetInt32());
            Assert.Equal("After edit", offer.GetProperty("title").GetString());
            Assert.Equal(8m, offer.GetProperty("discountAmount").GetDecimal());
            Assert.Equal("30_days_after_issue", offer.GetProperty("validity").GetString());
            Assert.Equal("New staff note.", offer.GetProperty("staffInstructions").GetString());
            Assert.Equal(1, offer.GetProperty("issueCount").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var issue = await context.OfferIssues
                .AsNoTracking()
                .SingleAsync(row => row.CatalogOfferId == offerId);
            Assert.Equal(5m, issue.DiscountAmount);
            Assert.Equal("Issued pass title", issue.Title);
        }

        [Fact]
        public async Task PutOffer_Returns400_WhenOfferTypeChanges()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-update-type");
            var offerId = await CreateOfferAsync(seeded, "Type locked");

            using var request = AuthorizedJson(
                HttpMethod.Put,
                $"/api/offers/{offerId}",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "percentage_discount",
                    title = "Type locked",
                    description = "A reusable campaign offer definition.",
                    validity = "14_days_after_issue",
                    discountPercentage = 10m,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Contains(
                "type",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PutOffer_Returns409_WhenExpiredOrArchived()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-update-status");
            var expiredId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Expired edit",
                status: "active",
                validity: CatalogOfferValidity.ChooseExpiryDate,
                customExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
            );
            var archivedId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Archived edit",
                status: "archived"
            );

            using var expiredRequest = AuthorizedJson(
                HttpMethod.Put,
                $"/api/offers/{expiredId}",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "fixed_discount",
                    title = "Expired edit",
                    description = "Seeded catalog offer for list tests.",
                    validity = "14_days_after_issue",
                    discountAmount = 5m,
                }
            );
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await _client.SendAsync(expiredRequest)).StatusCode
            );

            using var archivedRequest = AuthorizedJson(
                HttpMethod.Put,
                $"/api/offers/{archivedId}",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "fixed_discount",
                    title = "Archived edit",
                    description = "Seeded catalog offer for list tests.",
                    validity = "14_days_after_issue",
                    discountAmount = 5m,
                }
            );
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await _client.SendAsync(archivedRequest)).StatusCode
            );
        }

        [Fact]
        public async Task PutOffer_AllowsDraftActiveAndPaused()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-update-allowed");
            var activeId = await CreateOfferAsync(seeded, "Active edit");
            var pausedId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Paused edit",
                status: "paused"
            );
            var draftId = await SeedOfferDirectAsync(
                seeded.LocationId,
                title: "Draft edit",
                status: "draft"
            );

            foreach (var offerId in new[] { activeId, pausedId, draftId })
            {
                using var request = AuthorizedJson(
                    HttpMethod.Put,
                    $"/api/offers/{offerId}",
                    seeded.Jwt,
                    new
                    {
                        locationId = seeded.LocationId,
                        offerType = "fixed_discount",
                        title = $"Updated {offerId}",
                        description = "Seeded catalog offer for list tests.",
                        validity = "14_days_after_issue",
                        discountAmount = 6m,
                    }
                );
                var response = await _client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(
                    6m,
                    (await ReadJsonAsync(response))
                        .GetProperty("offer")
                        .GetProperty("discountAmount")
                        .GetDecimal()
                );
            }
        }

        private async Task<int> CreateOfferAsync(
            (string Jwt, int LocationId) seeded,
            string title
        )
        {
            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    offerType = "fixed_discount",
                    title,
                    description = "A reusable campaign offer definition.",
                    validity = "14_days_after_issue",
                    discountAmount = 5m,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await ReadJsonAsync(response))
                .GetProperty("offer")
                .GetProperty("id")
                .GetInt32();
        }

        private async Task<int> SeedOfferDirectAsync(
            int locationId,
            string title,
            string status,
            CatalogOfferValidity validity = CatalogOfferValidity.Days14AfterIssue,
            DateOnly? customExpiryDate = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var entity = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = status,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Seeded catalog offer for list tests.",
                Validity = validity,
                CustomExpiryDate = customExpiryDate,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        private async Task<int> SeedLocationGuestAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var location = await context.RestaurantLocations
                .FindAsync(locationId);
            Assert.NotNull(location);

            var master = new MasterGuest
            {
                RestaurantId = location!.RestaurantId,
                Email = $"offers-guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                CreatedAt = now,
            };
            context.LocationGuests.Add(lg);
            await context.SaveChangesAsync();
            return lg.Id;
        }

        private async Task SeedOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            string claimCode,
            decimal discountAmount
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            context.OfferIssues.Add(new OfferIssue
            {
                CatalogOfferId = catalogOfferId,
                LocationGuestId = locationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = now,
                ClaimedAtUtc = now,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = now.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "Issued pass title",
                Description = "Issued pass description",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = discountAmount,
            });
            await context.SaveChangesAsync();
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
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

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithLocationAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Offers Catalog Owner",
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
                Name = "Offers Catalog Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

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
    }
}

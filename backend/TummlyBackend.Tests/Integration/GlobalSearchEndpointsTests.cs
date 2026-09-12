using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GlobalSearchEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GlobalSearchEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetSearch_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/search?q=mo&locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSearch_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("gs-owner-a-token-1234567890");
            var other = await SeedOwnerAsync(
                "gs-owner-b-token-1234567890",
                email: "gs-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(other.LocationId, "mo", types: "guests")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetSearch_Returns404_ForUnknownLocation()
        {
            var owner = await SeedOwnerAsync("gs-unknown-loc-token-12345");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(999_999, "mo", types: "guests")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyGroups_WhenQueryShorterThanTwo()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "gs-short-q-token-123456789",
                guestName: "Mohamed",
                email: "mo@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "m", types: "guests")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("m", body.GetProperty("q").GetString());
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("guests", groups[0].GetProperty("type").GetString());
            Assert.Equal(0, groups[0].GetProperty("hits").GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_ReturnsGuestHits_ForOwnerMatch()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "gs-owner-match-token-123456",
                guestName: "Mohamed",
                email: "a@example.com",
                locationName: "Camden"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "mo", types: "guests")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("mo", body.GetProperty("q").GetString());
            Assert.Equal(
                seeded.LocationId,
                body.GetProperty("locationId").GetInt32()
            );

            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("guests", groups[0].GetProperty("type").GetString());

            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            var hit = hits[0];
            Assert.Equal(
                seeded.LocationGuestId.ToString(),
                hit.GetProperty("id").GetString()
            );
            Assert.Equal("guest", hit.GetProperty("entityType").GetString());
            Assert.Equal("Mohamed", hit.GetProperty("title").GetString());
            Assert.Equal("a@example.com", hit.GetProperty("subtitle").GetString());
            Assert.Equal(
                seeded.LocationId,
                hit.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Camden", hit.GetProperty("locationName").GetString());
            Assert.Equal(
                "Eligible — Email",
                hit.GetProperty("status").GetString()
            );
            Assert.False(hit.TryGetProperty("email", out _));
            Assert.False(hit.TryGetProperty("mobile", out _));
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantGuests()
        {
            var ownerA = await SeedOwnerWithGuestAsync(
                "gs-tenant-a-token-123456789",
                guestName: "Mohamed",
                email: "a-mo@example.com"
            );
            var ownerB = await SeedOwnerWithGuestAsync(
                "gs-tenant-b-token-123456789",
                guestName: "Mohamed",
                email: "b-mo@example.com",
                emailUser: "gs-tenant-b-owner@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "mo", types: "guests")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerA.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                ownerA.LocationGuestId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.Equal(
                "a-mo@example.com",
                hits[0].GetProperty("subtitle").GetString()
            );
            Assert.NotEqual(
                ownerB.LocationGuestId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ScopesHitsToShellLocation_NotSiblingLocation()
        {
            var seeded = await SeedMultiLocationGuestsAsync(
                "gs-multi-loc-token-12345678"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationAId, "Guest", types: "guests")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                "Location A Guest",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationAId,
                hits[0].GetProperty("locationId").GetInt32()
            );
        }

        [Fact]
        public async Task GetSearch_ScopeAll_ReturnsGuestsFromAllAuthorisedLocations()
        {
            var seeded = await SeedMultiLocationGuestsAsync(
                "gs-scope-all-owner-token-12"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(
                    seeded.LocationAId,
                    "Guest",
                    types: "guests",
                    scope: "all"
                )
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                seeded.LocationAId,
                body.GetProperty("locationId").GetInt32()
            );
            Assert.False(body.TryGetProperty("total", out _));
            Assert.False(body.TryGetProperty("hiddenCount", out _));
            Assert.False(body.TryGetProperty("totalFilteredCount", out _));

            var hits = body.GetProperty("groups")[0].GetProperty("hits");
            Assert.Equal(2, hits.GetArrayLength());

            var titles = hits
                .EnumerateArray()
                .Select(hit => hit.GetProperty("title").GetString()!)
                .OrderBy(title => title)
                .ToArray();
            Assert.Equal(
                new[] { "Location A Guest", "Location B Guest" },
                titles
            );

            var byTitle = hits
                .EnumerateArray()
                .ToDictionary(
                    hit => hit.GetProperty("title").GetString()!,
                    hit => hit
                );
            Assert.Equal(
                seeded.LocationAId,
                byTitle["Location A Guest"].GetProperty("locationId").GetInt32()
            );
            Assert.Equal(
                "Camden Street",
                byTitle["Location A Guest"].GetProperty("locationName").GetString()
            );
            Assert.Equal(
                seeded.LocationBId,
                byTitle["Location B Guest"].GetProperty("locationId").GetInt32()
            );
            Assert.Equal(
                "Second Street",
                byTitle["Location B Guest"].GetProperty("locationName").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ScopeAll_ScopedManager_ExcludesOutOfScopeLocation_NoExistenceLeak()
        {
            var seeded = await SeedLocationManagerWithGuestsAsync(
                "gs-scope-all-mgr-token-12"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(
                    seeded.InScopeLocationId,
                    "Guest",
                    types: "guests",
                    scope: "all"
                )
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.TryGetProperty("total", out _));
            Assert.False(body.TryGetProperty("hiddenCount", out _));
            Assert.False(body.TryGetProperty("totalFilteredCount", out _));

            var hits = body.GetProperty("groups")[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                "In Scope Guest",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.InScopeLocationId,
                hits[0].GetProperty("locationId").GetInt32()
            );
            Assert.DoesNotContain(
                hits.EnumerateArray(),
                hit =>
                    hit.GetProperty("title").GetString()
                    == "Out Of Scope Guest"
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyGuestHits_WhenStaffHasGuestsNoAccess()
        {
            var seeded = await SeedOwnerAndStaffMemberAsync(
                seedMatchingGuest: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.InScopeLocationId, "Scope", types: "guests")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var hits = body.GetProperty("groups")[0].GetProperty("hits");
            Assert.Equal(0, hits.GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_ReturnsCampaignHits_ForOwnerNameMatch()
        {
            var seeded = await SeedOwnerWithCampaignAsync(
                "gs-campaign-match-token-12",
                campaignName: "Weekend brunch push",
                status: "draft",
                channel: "email",
                locationName: "Camden"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "week", types: "campaigns")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("campaigns", groups[0].GetProperty("type").GetString());

            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            var hit = hits[0];
            Assert.Equal(
                seeded.CampaignId.ToString(),
                hit.GetProperty("id").GetString()
            );
            Assert.Equal("campaign", hit.GetProperty("entityType").GetString());
            Assert.Equal(
                "Weekend brunch push",
                hit.GetProperty("title").GetString()
            );
            Assert.Equal("Email", hit.GetProperty("subtitle").GetString());
            Assert.Equal(
                seeded.LocationId,
                hit.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Camden", hit.GetProperty("locationName").GetString());
            Assert.Equal("Draft", hit.GetProperty("status").GetString());
            Assert.False(hit.TryGetProperty("billingReservationRef", out _));
            Assert.False(hit.TryGetProperty("messageBody", out _));
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantCampaigns()
        {
            var ownerA = await SeedOwnerWithCampaignAsync(
                "gs-camp-tenant-a-token-123",
                campaignName: "Shared name brunch",
                status: "scheduled",
                channel: "sms"
            );
            var ownerB = await SeedOwnerWithCampaignAsync(
                "gs-camp-tenant-b-token-123",
                campaignName: "Shared name brunch",
                status: "draft",
                channel: "email",
                emailUser: "gs-camp-tenant-b@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "Shared", types: "campaigns")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerA.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                ownerA.CampaignId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.NotEqual(
                ownerB.CampaignId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ScopesCampaignHitsToShellLocation_NotSibling()
        {
            var seeded = await SeedMultiLocationCampaignsAsync(
                "gs-multi-camp-token-12345"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationAId, "Campaign", types: "campaigns")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                "Location A Campaign",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationAId,
                hits[0].GetProperty("locationId").GetInt32()
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyCampaignHits_WhenStaffHasCampaignsNoAccess()
        {
            var seeded = await SeedOwnerAndStaffMemberAsync(
                seedMatchingGuest: false,
                seedMatchingCampaign: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.InScopeLocationId, "Scope", types: "campaigns")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("campaigns", groups[0].GetProperty("type").GetString());
            Assert.Equal(0, groups[0].GetProperty("hits").GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_ReturnsOfferHits_ForOwnerTitleMatch()
        {
            var seeded = await SeedOwnerWithOfferAsync(
                "gs-offer-match-token-12345",
                offerTitle: "Monday Lunch Deal",
                locationName: "Camden"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "lunch", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("lunch", body.GetProperty("q").GetString());
            Assert.Equal(
                seeded.LocationId,
                body.GetProperty("locationId").GetInt32()
            );

            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("offers", groups[0].GetProperty("type").GetString());

            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            var hit = hits[0];
            Assert.Equal(
                seeded.OfferId.ToString(),
                hit.GetProperty("id").GetString()
            );
            Assert.Equal("offer", hit.GetProperty("entityType").GetString());
            Assert.Equal(
                "Monday Lunch Deal",
                hit.GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationId,
                hit.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Camden", hit.GetProperty("locationName").GetString());
            Assert.Equal("Active", hit.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantOffers()
        {
            var ownerA = await SeedOwnerWithOfferAsync(
                "gs-offer-tenant-a-token-123",
                offerTitle: "Shared Title Deal"
            );
            var ownerB = await SeedOwnerWithOfferAsync(
                "gs-offer-tenant-b-token-123",
                offerTitle: "Shared Title Deal",
                emailUser: "gs-offer-tenant-b@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "Shared", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerA.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                ownerA.OfferId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.NotEqual(
                ownerB.OfferId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ScopesOfferHitsToShellLocation_NotSiblingLocation()
        {
            var seeded = await SeedMultiLocationOffersAsync(
                "gs-offer-multi-loc-token-12"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationAId, "Deal", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                "Location A Deal",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationAId,
                hits[0].GetProperty("locationId").GetInt32()
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyOfferHits_WhenBillingAdminHasOffersNoAccess()
        {
            var seeded = await SeedOwnerAndBillingAdminWithOfferAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Lunch", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("offers", groups[0].GetProperty("type").GetString());
            Assert.Equal(0, groups[0].GetProperty("hits").GetArrayLength());
        }


        [Fact]
        public async Task GetSearch_ReturnsActiveAndPausedQrHits_ExcludesArchived()
        {
            var seeded = await SeedOwnerWithQrCodesAsync(
                "gs-qr-status-token-1234567"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "table", types: "qr-codes")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("qr-codes", groups[0].GetProperty("type").GetString());

            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            var hit = hits[0];
            Assert.Equal(
                seeded.ActiveTableTentId.ToString(),
                hit.GetProperty("id").GetString()
            );
            Assert.Equal("qr-code", hit.GetProperty("entityType").GetString());
            Assert.Equal("Table tent", hit.GetProperty("title").GetString());
            Assert.Equal("Active", hit.GetProperty("status").GetString());
            Assert.Equal(
                seeded.LocationId,
                hit.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Camden", hit.GetProperty("locationName").GetString());

            using var pausedRequest = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "window", types: "qr-codes")
            );
            pausedRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var pausedResponse = await _client.SendAsync(pausedRequest);
            Assert.Equal(HttpStatusCode.OK, pausedResponse.StatusCode);
            var pausedHits = (await ReadJsonAsync(pausedResponse))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, pausedHits.GetArrayLength());
            Assert.Equal(
                seeded.PausedWindowId.ToString(),
                pausedHits[0].GetProperty("id").GetString()
            );
            Assert.Equal(
                "Paused",
                pausedHits[0].GetProperty("status").GetString()
            );

            using var archivedRequest = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "offer", types: "qr-codes")
            );
            archivedRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var archivedResponse = await _client.SendAsync(archivedRequest);
            Assert.Equal(HttpStatusCode.OK, archivedResponse.StatusCode);
            var archivedHits = (await ReadJsonAsync(archivedResponse))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(0, archivedHits.GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_MatchesDigitalGuestLinkName_ForQrHits()
        {
            var seeded = await SeedOwnerWithQrCodesAsync(
                "gs-qr-link-token-123456789"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "instagram", types: "qr-codes")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                seeded.DigitalLinkId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.Equal(
                "Instagram bio",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal("Active", hits[0].GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantQrCodes()
        {
            var ownerA = await SeedOwnerWithQrCodesAsync(
                "gs-qr-tenant-a-token-12345"
            );
            var ownerB = await SeedOwnerWithQrCodesAsync(
                "gs-qr-tenant-b-token-12345"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "table", types: "qr-codes")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerA.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                ownerA.ActiveTableTentId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.NotEqual(
                ownerB.ActiveTableTentId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyQrHits_WhenStaffHasCaptureNoAccess()
        {
            var seeded = await SeedOwnerAndStaffMemberWithQrAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(
                    seeded.InScopeLocationId,
                    "table",
                    types: "qr-codes"
                )
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("groups").GetArrayLength());
            Assert.Equal(
                "qr-codes",
                body.GetProperty("groups")[0].GetProperty("type").GetString()
            );
            Assert.Equal(
                0,
                body.GetProperty("groups")[0]
                    .GetProperty("hits")
                    .GetArrayLength()
            );
        }

        [Fact]
        public async Task GetSearch_ProjectsEffectiveExpiredStatus_NotStoredActive()
        {
            var seeded = await SeedOwnerWithOfferAsync(
                "gs-offer-expired-token-1234",
                offerTitle: "Expired Lunch Deal",
                status: CatalogOfferStatus.Active,
                validity: CatalogOfferValidity.ChooseExpiryDate,
                customExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3))
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Expired", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hit = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits")[0];
            Assert.Equal("Expired", hit.GetProperty("status").GetString());
            Assert.Equal(
                "Expired Lunch Deal",
                hit.GetProperty("title").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_DoesNotMutateOfferStatus_WhenSearching()
        {
            var seeded = await SeedOwnerWithOfferAsync(
                "gs-offer-readonly-token-123",
                offerTitle: "Stable Draft Deal",
                status: CatalogOfferStatus.Draft
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Stable", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var offer = await context.CatalogOffers.FindAsync(seeded.OfferId);
            Assert.NotNull(offer);
            Assert.Equal(CatalogOfferStatus.Draft, offer!.Status);
        }

        [Fact]
        public async Task GetSearch_MatchesOfferViaAttachedCampaignName()
        {
            var seeded = await SeedOwnerWithOfferAsync(
                "gs-offer-camp-name-token-12",
                offerTitle: "Quiet Title Offer"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            context.Campaigns.Add(new Campaign
            {
                RestaurantLocationId = seeded.LocationId,
                OfferId = seeded.OfferId,
                Name = "Brunch Blast Campaign",
                Status = "draft",
                Channel = "email",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Brunch", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                seeded.OfferId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.Equal(
                "Quiet Title Offer",
                hits[0].GetProperty("title").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsFeedbackHits_ForAuthorisedLocationMatches()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "gs-fb-match-token-1234567",
                guestName: "Sam Guest",
                comment: "Cold chips at the counter",
                locationName: "Camden",
                detectedTagsJson: "[\"WaitTime\"]",
                sentiment: FeedbackSentiment.Negative
            );

            // Comment match
            using (var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "cold", types: "feedback")
            ))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                var response = await _client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var hit = AssertSingleFeedbackHit(await ReadJsonAsync(response));
                Assert.Equal(seeded.FeedbackId.ToString(), hit.GetProperty("id").GetString());
                Assert.Equal("feedback", hit.GetProperty("entityType").GetString());
                Assert.Equal("Sam Guest", hit.GetProperty("title").GetString());
                Assert.Equal(
                    "Cold chips at the counter",
                    hit.GetProperty("subtitle").GetString()
                );
                Assert.Equal(
                    seeded.LocationId,
                    hit.GetProperty("locationId").GetInt32()
                );
                Assert.Equal("Camden", hit.GetProperty("locationName").GetString());
                Assert.Equal("New", hit.GetProperty("status").GetString());
            }

            // Guest name match
            using (var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Sam", types: "feedback")
            ))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                var hit = AssertSingleFeedbackHit(
                    await ReadJsonAsync(await _client.SendAsync(request))
                );
                Assert.Equal(seeded.FeedbackId.ToString(), hit.GetProperty("id").GetString());
            }

            // Numeric id match
            using (var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(
                    seeded.LocationId,
                    seeded.FeedbackId.ToString(),
                    types: "feedback"
                )
            ))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                var hit = AssertSingleFeedbackHit(
                    await ReadJsonAsync(await _client.SendAsync(request))
                );
                Assert.Equal(seeded.FeedbackId.ToString(), hit.GetProperty("id").GetString());
            }

            // FDB-style identity match
            var fdb =
                $"FDB-{seeded.FeedbackId.ToString().PadLeft(6, '0')}";
            using (var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, fdb, types: "feedback")
            ))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                var hit = AssertSingleFeedbackHit(
                    await ReadJsonAsync(await _client.SendAsync(request))
                );
                Assert.Equal(seeded.FeedbackId.ToString(), hit.GetProperty("id").GetString());
            }

            // Governed tag label match
            using (var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "wait", types: "feedback")
            ))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                var hit = AssertSingleFeedbackHit(
                    await ReadJsonAsync(await _client.SendAsync(request))
                );
                Assert.Equal(seeded.FeedbackId.ToString(), hit.GetProperty("id").GetString());
            }
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantOrSiblingLocationFeedback()
        {
            var ownerA = await SeedOwnerWithFeedbackAsync(
                "gs-fb-tenant-a-token-12345",
                guestName: "Shared Name",
                comment: "Shared comment text",
                emailUser: "gs-fb-a@example.com"
            );
            var ownerB = await SeedOwnerWithFeedbackAsync(
                "gs-fb-tenant-b-token-12345",
                guestName: "Shared Name",
                comment: "Shared comment text",
                emailUser: "gs-fb-b@example.com"
            );
            var multi = await SeedMultiLocationFeedbackAsync(
                "gs-fb-multi-loc-token-123"
            );

            using (var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "Shared", types: "feedback")
            ))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", ownerA.Jwt);
                var hit = AssertSingleFeedbackHit(
                    await ReadJsonAsync(await _client.SendAsync(request))
                );
                Assert.Equal(
                    ownerA.FeedbackId.ToString(),
                    hit.GetProperty("id").GetString()
                );
                Assert.NotEqual(
                    ownerB.FeedbackId.ToString(),
                    hit.GetProperty("id").GetString()
                );
            }

            using (var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(multi.LocationAId, "Sibling", types: "feedback")
            ))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", multi.Jwt);
                var hit = AssertSingleFeedbackHit(
                    await ReadJsonAsync(await _client.SendAsync(request))
                );
                Assert.Equal(
                    multi.FeedbackAId.ToString(),
                    hit.GetProperty("id").GetString()
                );
                Assert.Equal(
                    multi.LocationAId,
                    hit.GetProperty("locationId").GetInt32()
                );
            }
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyFeedbackHits_WhenStaffHasFeedbackNoAccess()
        {
            var seeded = await SeedOwnerAndStaffMemberWithFeedbackAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.InScopeLocationId, "Scope", types: "feedback")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("feedback", groups[0].GetProperty("type").GetString());
            Assert.Equal(0, groups[0].GetProperty("hits").GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_ReturnsAllDefaultGroups_WhenTypesOmitted()
        {
            var seeded = await SeedOwnerWithGuestAndFeedbackAsync(
                "gs-both-groups-token-12345"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Mo")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var groups = (await ReadJsonAsync(response)).GetProperty("groups");
            Assert.Equal(5, groups.GetArrayLength());
            Assert.Equal("guests", groups[0].GetProperty("type").GetString());
            Assert.Equal("feedback", groups[1].GetProperty("type").GetString());
            Assert.Equal("campaigns", groups[2].GetProperty("type").GetString());
            Assert.Equal("offers", groups[3].GetProperty("type").GetString());
            Assert.Equal("qr-codes", groups[4].GetProperty("type").GetString());
            Assert.True(groups[0].GetProperty("hits").GetArrayLength() >= 1);
            Assert.True(groups[1].GetProperty("hits").GetArrayLength() >= 1);
        }

        private static JsonElement AssertSingleFeedbackHit(JsonElement body)
        {
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("feedback", groups[0].GetProperty("type").GetString());
            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            return hits[0];
        }

        private static string SearchUrl(
            int locationId,
            string q,
            string? types = null,
            int? limit = null,
            string? scope = null
        )
        {
            var url =
                $"/api/search?locationId={locationId}&q={Uri.EscapeDataString(q)}";
            if (types != null)
            {
                url += $"&types={Uri.EscapeDataString(types)}";
            }
            if (limit != null)
            {
                url += $"&limit={limit.Value}";
            }
            if (scope != null)
            {
                url += $"&scope={Uri.EscapeDataString(scope)}";
            }
            return url;
        }

        private async Task<(string Jwt, int LocationId, int RestaurantId)> SeedOwnerAsync(
            string linkToken,
            string email = "gs-owner@example.com",
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
                FullName = "Global Search Owner",
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
                Name = "Global Search Venue",
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

            // linkToken reserved for uniqueness / future activation seeds
            _ = linkToken;

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<OwnerGuestSeed> SeedOwnerWithGuestAsync(
            string linkToken,
            string guestName,
            string email,
            string locationName = "Camden Street",
            string? emailUser = null
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: emailUser ?? $"{Guid.NewGuid():N}@example.com",
                locationName: locationName
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = owner.RestaurantId,
                Email = email,
                NormalizedEmail = email.ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = owner.LocationId,
                Name = guestName,
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            return new OwnerGuestSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                locationGuest.Id
            );
        }

        private async Task<MultiLocationSeed> SeedMultiLocationGuestsAsync(
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
                FullName = "GS Multi Owner",
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
                Name = "GS Multi Venue",
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

            context.LocationGuests.AddRange(
                new LocationGuest
                {
                    MasterGuestId = masterA.Id,
                    RestaurantLocationId = locationA.Id,
                    Name = "Location A Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                },
                new LocationGuest
                {
                    MasterGuestId = masterB.Id,
                    RestaurantLocationId = locationB.Id,
                    Name = "Location B Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new MultiLocationSeed(jwt, locationA.Id, locationB.Id);
        }

        private async Task<OwnerCampaignSeed> SeedOwnerWithCampaignAsync(
            string linkToken,
            string campaignName,
            string status,
            string channel,
            string locationName = "Camden Street",
            string? emailUser = null
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: emailUser ?? $"{Guid.NewGuid():N}@example.com",
                locationName: locationName
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var campaign = new Campaign
            {
                RestaurantLocationId = owner.LocationId,
                Name = campaignName,
                Status = status,
                Channel = channel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            return new OwnerCampaignSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                campaign.Id
            );
        }

        private async Task<MultiLocationSeed> SeedMultiLocationCampaignsAsync(
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
                FullName = "GS Multi Camp Owner",
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
                Name = "GS Multi Camp Venue",
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

            context.Campaigns.AddRange(
                new Campaign
                {
                    RestaurantLocationId = locationA.Id,
                    Name = "Location A Campaign",
                    Status = "draft",
                    Channel = "email",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                },
                new Campaign
                {
                    RestaurantLocationId = locationB.Id,
                    Name = "Location B Campaign",
                    Status = "sent",
                    Channel = "sms",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new MultiLocationSeed(jwt, locationA.Id, locationB.Id);
        }

        private async Task<StaffSeed> SeedOwnerAndStaffMemberAsync(
            bool seedMatchingGuest,
            bool seedMatchingCampaign = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "GS Scope Owner",
                Email = $"gs-owner-12-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "GS Scope Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(inScope, outOfScope);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "GS Staff Member",
                Email = $"gs-staff-12-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900113",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Staff,
                LocationScope = LocationScopeKind.NamedList,
                NamedLocationIdsJson =
                    MembershipLocationScope.SerializeNamedIds([inScope.Id]),
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            if (seedMatchingGuest)
            {
                var email = $"{Guid.NewGuid():N}@example.com";
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = email,
                    NormalizedEmail = email,
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                context.LocationGuests.Add(new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = inScope.Id,
                    Name = "In Scope Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow,
                });
                await context.SaveChangesAsync();
            }

            if (seedMatchingCampaign)
            {
                context.Campaigns.Add(new Campaign
                {
                    RestaurantLocationId = inScope.Id,
                    Name = "In Scope Campaign",
                    Status = "draft",
                    Channel = "email",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
                await context.SaveChangesAsync();
            }

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new StaffSeed(memberJwt, inScope.Id);
        }

        private async Task<OwnerOfferSeed> SeedOwnerWithOfferAsync(
            string linkToken,
            string offerTitle,
            string status = CatalogOfferStatus.Active,
            CatalogOfferValidity validity = CatalogOfferValidity.Days14AfterIssue,
            DateOnly? customExpiryDate = null,
            string locationName = "Camden Street",
            string? emailUser = null
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: emailUser ?? $"{Guid.NewGuid():N}@example.com",
                locationName: locationName
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;
            var offer = new CatalogOffer
            {
                RestaurantLocationId = owner.LocationId,
                Status = status,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = offerTitle,
                Description = "Global Search offer seed.",
                Validity = validity,
                CustomExpiryDate = customExpiryDate,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();

            return new OwnerOfferSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                offer.Id
            );
        }

        private async Task<MultiLocationSeed> SeedMultiLocationOffersAsync(
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
                FullName = "GS Offer Multi Owner",
                Email = $"{linkTokenPrefix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900112",
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
                Name = "GS Offer Multi Venue",
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

            var now = DateTime.UtcNow;
            context.CatalogOffers.AddRange(
                new CatalogOffer
                {
                    RestaurantLocationId = locationA.Id,
                    Status = CatalogOfferStatus.Active,
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "Location A Deal",
                    Description = "A",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2),
                },
                new CatalogOffer
                {
                    RestaurantLocationId = locationB.Id,
                    Status = CatalogOfferStatus.Active,
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "Location B Deal",
                    Description = "B",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-3),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new MultiLocationSeed(jwt, locationA.Id, locationB.Id);
        }

        private async Task<BillingAdminOfferSeed> SeedOwnerAndBillingAdminWithOfferAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "GS Offer Scope Owner",
                Email = $"gs-offer-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900114",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "GS Offer Scope Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "GS Billing Admin",
                Email = $"gs-billing-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900115",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.BillingAdmin,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var now = DateTime.UtcNow;
            context.CatalogOffers.Add(new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "Lunch Special",
                Description = "NoAccess check",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await context.SaveChangesAsync();

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new BillingAdminOfferSeed(memberJwt, location.Id);
        }

        private async Task<OwnerQrSeed> SeedOwnerWithQrCodesAsync(
            string linkToken
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: $"{Guid.NewGuid():N}@example.com",
                locationName: "Camden"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var activeTableTent = new QrCode
            {
                RestaurantLocationId = owner.LocationId,
                QrType = QrType.TableTent,
                Token = $"{linkToken}-table-tent-token12",
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            };
            var pausedWindow = new QrCode
            {
                RestaurantLocationId = owner.LocationId,
                QrType = QrType.WindowSticker,
                Token = $"{linkToken}-window-token123456",
                Status = QrCodeStatus.Paused,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
            };
            var archivedOffer = new QrCode
            {
                RestaurantLocationId = owner.LocationId,
                QrType = QrType.OfferCard,
                Token = $"{linkToken}-offer-token1234567",
                Status = QrCodeStatus.Archived,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                ArchivedAt = DateTime.UtcNow.AddDays(-1),
            };
            var digitalLink = new QrCode
            {
                RestaurantLocationId = owner.LocationId,
                QrType = QrType.DigitalGuestLink,
                Token = $"{linkToken}-digital-token12345",
                Status = QrCodeStatus.Active,
                LinkName = "Instagram bio",
                NormalizedLinkName = "instagram bio",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            };
            context.QrCodes.AddRange(
                activeTableTent,
                pausedWindow,
                archivedOffer,
                digitalLink
            );
            await context.SaveChangesAsync();

            return new OwnerQrSeed(
                owner.Jwt,
                owner.LocationId,
                activeTableTent.Id,
                pausedWindow.Id,
                digitalLink.Id
            );
        }

        private async Task<StaffSeed> SeedOwnerAndStaffMemberWithQrAsync()
        {
            var seeded = await SeedOwnerAndStaffMemberAsync(
                seedMatchingGuest: false
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = seeded.InScopeLocationId,
                    QrType = QrType.TableTent,
                    Token = $"gs-staff-qr-table-tent-token1",
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            return seeded;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private async Task<OwnerFeedbackSeed> SeedOwnerWithFeedbackAsync(
            string linkToken,
            string guestName,
            string comment,
            string locationName = "Camden Street",
            string? emailUser = null,
            string? detectedTagsJson = null,
            FeedbackSentiment? sentiment = null
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: emailUser ?? $"{Guid.NewGuid():N}@example.com",
                locationName: locationName
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var classificationSucceeded =
                sentiment != null || detectedTagsJson != null;
            var feedback = new Feedback
            {
                RestaurantLocationId = owner.LocationId,
                GuestName = guestName,
                GuestContact = "guest@example.com",
                ContactType = ContactType.Email,
                Comment = comment,
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = classificationSucceeded
                    ? ClassificationStatus.Succeeded
                    : ClassificationStatus.Pending,
                Sentiment = sentiment,
                DetectedTagsJson = classificationSucceeded
                    ? detectedTagsJson ?? "[]"
                    : null,
                WorkflowStatus = FeedbackWorkflowStatus.New,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            return new OwnerFeedbackSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                feedback.Id
            );
        }

        private async Task<MultiLocationFeedbackSeed> SeedMultiLocationFeedbackAsync(
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
                FullName = "GS FB Multi Owner",
                Email = $"{linkTokenPrefix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900112",
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
                Name = "GS FB Multi Venue",
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

            var feedbackA = new Feedback
            {
                RestaurantLocationId = locationA.Id,
                GuestName = "Sibling A",
                GuestContact = "a@example.com",
                ContactType = ContactType.Email,
                Comment = "Sibling location A note",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                WorkflowStatus = FeedbackWorkflowStatus.New,
            };
            var feedbackB = new Feedback
            {
                RestaurantLocationId = locationB.Id,
                GuestName = "Sibling B",
                GuestContact = "b@example.com",
                ContactType = ContactType.Email,
                Comment = "Sibling location B note",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                WorkflowStatus = FeedbackWorkflowStatus.New,
            };
            context.Feedbacks.AddRange(feedbackA, feedbackB);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new MultiLocationFeedbackSeed(
                jwt,
                locationA.Id,
                locationB.Id,
                feedbackA.Id,
                feedbackB.Id
            );
        }

        private async Task<StaffSeed> SeedOwnerAndStaffMemberWithFeedbackAsync()
        {
            var seeded = await SeedOwnerAndStaffMemberAsync(
                seedMatchingGuest: false
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.Feedbacks.Add(new Feedback
            {
                RestaurantLocationId = seeded.InScopeLocationId,
                GuestName = "In Scope Feedback Guest",
                GuestContact = "scope@example.com",
                ContactType = ContactType.Email,
                Comment = "Scope feedback comment",
                CreatedAt = DateTime.UtcNow,
                WorkflowStatus = FeedbackWorkflowStatus.New,
            });
            await context.SaveChangesAsync();

            return seeded;
        }

        private async Task<OwnerGuestAndFeedbackSeed> SeedOwnerWithGuestAndFeedbackAsync(
            string linkToken
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: $"{Guid.NewGuid():N}@example.com",
                locationName: "Camden"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = owner.RestaurantId,
                Email = "mo-guest@example.com",
                NormalizedEmail = "mo-guest@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            context.LocationGuests.Add(new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = owner.LocationId,
                Name = "Morgan Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            });

            context.Feedbacks.Add(new Feedback
            {
                RestaurantLocationId = owner.LocationId,
                GuestName = "Morgan Feedback",
                GuestContact = "mo-fb@example.com",
                ContactType = ContactType.Email,
                Comment = "Morning service was slow",
                CreatedAt = DateTime.UtcNow,
                WorkflowStatus = FeedbackWorkflowStatus.New,
            });
            await context.SaveChangesAsync();

            return new OwnerGuestAndFeedbackSeed(owner.Jwt, owner.LocationId);
        }

        private sealed record OwnerGuestSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int LocationGuestId
        );

        private sealed record OwnerFeedbackSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int FeedbackId
        );

        private sealed record OwnerGuestAndFeedbackSeed(
            string Jwt,
            int LocationId
        );

        private sealed record MultiLocationFeedbackSeed(
            string Jwt,
            int LocationAId,
            int LocationBId,
            int FeedbackAId,
            int FeedbackBId
        );

        private sealed record OwnerCampaignSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int CampaignId
        );

        private sealed record OwnerOfferSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int OfferId
        );


        private sealed record OwnerQrSeed(
            string Jwt,
            int LocationId,
            int ActiveTableTentId,
            int PausedWindowId,
            int DigitalLinkId
        );

        private sealed record MultiLocationSeed(
            string Jwt,
            int LocationAId,
            int LocationBId
        );

        private sealed record StaffSeed(
            string MemberJwt,
            int InScopeLocationId
        );

        private sealed record BillingAdminOfferSeed(
            string MemberJwt,
            int LocationId
        );

        private async Task<ScopedManagerSeed> SeedLocationManagerWithGuestsAsync(
            string linkTokenPrefix
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "GS Mgr Owner",
                Email = $"{linkTokenPrefix}-owner@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900221",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "GS Mgr Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(inScope, outOfScope);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "GS Location Manager",
                Email = $"{linkTokenPrefix}-mgr@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900223",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.LocationManager,
                LocationScope = LocationScopeKind.NamedList,
                NamedLocationIdsJson =
                    MembershipLocationScope.SerializeNamedIds([inScope.Id]),
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            var masterIn = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"{linkTokenPrefix}-in@example.com",
                NormalizedEmail = $"{linkTokenPrefix}-in@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            var masterOut = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"{linkTokenPrefix}-out@example.com",
                NormalizedEmail = $"{linkTokenPrefix}-out@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.AddRange(masterIn, masterOut);
            await context.SaveChangesAsync();

            context.LocationGuests.AddRange(
                new LocationGuest
                {
                    MasterGuestId = masterIn.Id,
                    RestaurantLocationId = inScope.Id,
                    Name = "In Scope Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                },
                new LocationGuest
                {
                    MasterGuestId = masterOut.Id,
                    RestaurantLocationId = outOfScope.Id,
                    Name = "Out Of Scope Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                }
            );
            await context.SaveChangesAsync();

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new ScopedManagerSeed(
                memberJwt,
                inScope.Id,
                outOfScope.Id
            );
        }

        private sealed record ScopedManagerSeed(
            string MemberJwt,
            int InScopeLocationId,
            int OutOfScopeLocationId
        );
    }
}

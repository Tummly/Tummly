using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackInboxEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackInboxEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetFeedbackInbox_TabsAndCounts_ScopeToLocationAndRangeOnly()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-inbox-tabs"
            );

            // Needs attention + New
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                "Needs attention new",
                "Alex",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New
            );
            // Needs attention + In progress
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                "Needs attention in progress",
                "Blair",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.InProgress
            );
            // Resolved negative — not Needs attention
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc),
                "Resolved negative",
                "Casey",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.Resolved
            );
            // Positive New
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                "Positive new",
                "Dana",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                FeedbackWorkflowStatus.New
            );
            // Outside range — must not inflate tab counts
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc),
                "Outside range",
                "Eve",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New
            );

            using var allRequest = AuthorizedGet(
                InboxUrl(seeded.LocationId, from, to, tab: "all"),
                seeded.Jwt
            );
            var allResponse = await _client.SendAsync(allRequest);
            Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            var allBody = await ReadJsonAsync(allResponse);

            Assert.True(allBody.GetProperty("success").GetBoolean());
            Assert.Equal(4, allBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(25, allBody.GetProperty("pageSize").GetInt32());

            var tabCounts = allBody.GetProperty("tabCounts");
            Assert.Equal(4, tabCounts.GetProperty("all").GetInt32());
            Assert.Equal(2, tabCounts.GetProperty("needsAttention").GetInt32());
            Assert.Equal(2, tabCounts.GetProperty("new").GetInt32());
            Assert.Equal(1, tabCounts.GetProperty("inProgress").GetInt32());
            Assert.Equal(1, tabCounts.GetProperty("resolved").GetInt32());

            using var needsRequest = AuthorizedGet(
                InboxUrl(
                    seeded.LocationId,
                    from,
                    to,
                    tab: "needs-attention"
                ),
                seeded.Jwt
            );
            var needsBody = await ReadJsonAsync(
                await _client.SendAsync(needsRequest)
            );
            Assert.Equal(2, needsBody.GetProperty("totalCount").GetInt32());
            // Tab counts stay range-scoped even when tab filter narrows items.
            Assert.Equal(
                4,
                needsBody.GetProperty("tabCounts").GetProperty("all").GetInt32()
            );

            using var newRequest = AuthorizedGet(
                InboxUrl(seeded.LocationId, from, to, tab: "new"),
                seeded.Jwt
            );
            var newBody = await ReadJsonAsync(
                await _client.SendAsync(newRequest)
            );
            Assert.Equal(2, newBody.GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task GetFeedbackInbox_SearchAndSentimentFilter_ComposeWithAnd()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-inbox-filters"
            );

            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                "Cold soup was awful",
                "Alex Soup",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New,
                detectedTagsJson: "[\"FoodQuality\"]"
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 13, 10, 0, 0, DateTimeKind.Utc),
                "Cold soup was fine",
                "Blair Soup",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                FeedbackWorkflowStatus.New,
                detectedTagsJson: "[\"FoodQuality\"]"
            );
            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc),
                "Service was slow",
                "Casey Wait",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Negative,
                FeedbackWorkflowStatus.New,
                detectedTagsJson: "[\"Service\"]"
            );

            using var request = AuthorizedGet(
                InboxUrl(
                    seeded.LocationId,
                    from,
                    to,
                    tab: "all",
                    q: "soup",
                    sentiment: ["negative"]
                ),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Cold soup was awful",
                body.GetProperty("items")[0].GetProperty("comment").GetString()
            );
            // Search/filters must not change tab counts.
            Assert.Equal(
                3,
                body.GetProperty("tabCounts").GetProperty("all").GetInt32()
            );
        }

        [Fact]
        public async Task GetFeedbackInbox_Pagination_UsesPageSize25_AndResetsOrderingNewestFirst()
        {
            var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithLocationAsync(
                "feedback-inbox-page"
            );

            for (var i = 0; i < 26; i++)
            {
                await AddFeedbackAsync(
                    seeded.LocationId,
                    new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc)
                        .AddHours(i),
                    $"Comment {i}",
                    $"Guest {i}",
                    ClassificationStatus.Succeeded,
                    FeedbackSentiment.Neutral,
                    FeedbackWorkflowStatus.New
                );
            }

            using var page1 = AuthorizedGet(
                InboxUrl(seeded.LocationId, from, to, page: 1),
                seeded.Jwt
            );
            var page1Body = await ReadJsonAsync(await _client.SendAsync(page1));
            Assert.Equal(26, page1Body.GetProperty("totalCount").GetInt32());
            Assert.Equal(25, page1Body.GetProperty("items").GetArrayLength());
            Assert.Equal(
                "Comment 25",
                page1Body.GetProperty("items")[0].GetProperty("comment").GetString()
            );

            using var page2 = AuthorizedGet(
                InboxUrl(seeded.LocationId, from, to, page: 2),
                seeded.Jwt
            );
            var page2Body = await ReadJsonAsync(await _client.SendAsync(page2));
            Assert.Equal(1, page2Body.GetProperty("items").GetArrayLength());
            Assert.Equal(
                "Comment 0",
                page2Body.GetProperty("items")[0].GetProperty("comment").GetString()
            );
        }

        [Fact]
        public async Task GetFeedbackInbox_ReturnsItemFields_ForInboxColumns()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var seeded = await SeedOwnerWithQrAsync("feedback-inbox-fields");

            await AddFeedbackAsync(
                seeded.LocationId,
                new DateTime(2026, 7, 12, 10, 0, 0, DateTimeKind.Utc),
                "Great meal",
                "Alex Guest",
                ClassificationStatus.Succeeded,
                FeedbackSentiment.Positive,
                FeedbackWorkflowStatus.InProgress,
                detectedTagsJson: "[\"Service\"]",
                qrCodeId: seeded.QrCodeId,
                contactType: ContactType.Email,
                guestContact: "alex@example.com"
            );

            using var request = AuthorizedGet(
                InboxUrl(seeded.LocationId, from, to),
                seeded.Jwt
            );
            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var item = body.GetProperty("items")[0];

            Assert.Equal("Great meal", item.GetProperty("comment").GetString());
            Assert.Equal("Alex Guest", item.GetProperty("guestName").GetString());
            Assert.Equal("positive", item.GetProperty("sentiment").GetString());
            Assert.Equal(
                "in_progress",
                item.GetProperty("workflowStatus").GetString()
            );
            Assert.False(item.GetProperty("needsAttention").GetBoolean());
            Assert.Equal("Main", item.GetProperty("locationName").GetString());
            Assert.Equal(
                "Counter card",
                item.GetProperty("qrSource").GetString()
            );
            Assert.Contains(
                "Service",
                item.GetProperty("detectedTags")
                    .EnumerateArray()
                    .Select(e => e.GetString())
            );
        }

        [Fact]
        public async Task GetFeedbackInbox_Returns401_WhenUnauthenticated()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                InboxUrl(1, from, to)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackInbox_Returns403_ForNonOwnedLocation()
        {
            var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc);
            var owner = await SeedOwnerWithLocationAsync("feedback-inbox-a");
            var other = await SeedOwnerWithLocationAsync("feedback-inbox-b");

            using var request = AuthorizedGet(
                InboxUrl(other.LocationId, from, to),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private static HttpRequestMessage AuthorizedGet(
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static string InboxUrl(
            int locationId,
            DateTime from,
            DateTime to,
            string tab = "all",
            string? q = null,
            string[]? sentiment = null,
            int page = 1,
            string sort = "newest-submitted"
        )
        {
            var fromIso = from.ToString("o", CultureInfo.InvariantCulture);
            var toIso = to.ToString("o", CultureInfo.InvariantCulture);
            var url =
                $"/api/feedback/inbox?locationId={locationId}"
                + $"&from={Uri.EscapeDataString(fromIso)}"
                + $"&to={Uri.EscapeDataString(toIso)}"
                + $"&tab={Uri.EscapeDataString(tab)}"
                + $"&sort={Uri.EscapeDataString(sort)}"
                + $"&page={page}"
                + "&pageSize=25";

            if (!string.IsNullOrWhiteSpace(q))
            {
                url += $"&q={Uri.EscapeDataString(q)}";
            }

            if (sentiment is { Length: > 0 })
            {
                foreach (var value in sentiment)
                {
                    url += $"&sentiment={Uri.EscapeDataString(value)}";
                }
            }

            return url;
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithLocationAsync(string emailLocalPart)
        {
            var seeded = await SeedOwnerWithQrAsync(emailLocalPart);
            return (seeded.Jwt, seeded.LocationId);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int QrCodeId
        )> SeedOwnerWithQrAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Inbox Owner",
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
                Name = "Inbox Venue",
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

            var qr = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Status = QrCodeStatus.Active,
                Token = $"tok-{emailLocalPart}-{Guid.NewGuid():N}"[..40],
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qr);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, qr.Id);
        }

        private async Task AddFeedbackAsync(
            int locationId,
            DateTime createdAt,
            string comment,
            string guestName,
            ClassificationStatus classificationStatus,
            FeedbackSentiment? sentiment,
            FeedbackWorkflowStatus workflowStatus,
            string? detectedTagsJson = null,
            int qrCodeId = 0,
            ContactType contactType = ContactType.Email,
            string guestContact = "alex@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            if (qrCodeId == 0)
            {
                var existing = await context.QrCodes
                    .Where(q => q.RestaurantLocationId == locationId)
                    .Select(q => q.Id)
                    .FirstOrDefaultAsync();
                if (existing == 0)
                {
                    var qr = new QrCode
                    {
                        RestaurantLocationId = locationId,
                        QrType = QrType.CounterCard,
                        Status = QrCodeStatus.Active,
                        Token = $"tok-fb-{Guid.NewGuid():N}"[..40],
                        CreatedAt = DateTime.UtcNow,
                    };
                    context.QrCodes.Add(qr);
                    await context.SaveChangesAsync();
                    qrCodeId = qr.Id;
                }
                else
                {
                    qrCodeId = existing;
                }
            }

            context.Feedbacks.Add(new Feedback
            {
                RestaurantLocationId = locationId,
                QrCodeId = qrCodeId,
                GuestName = guestName,
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = comment,
                CreatedAt = createdAt,
                ClassificationStatus = classificationStatus,
                Sentiment = sentiment,
                DetectedTagsJson =
                    detectedTagsJson
                    ?? (classificationStatus == ClassificationStatus.Succeeded
                        ? "[]"
                        : null),
                WorkflowStatus = workflowStatus,
            });
            await context.SaveChangesAsync();
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}

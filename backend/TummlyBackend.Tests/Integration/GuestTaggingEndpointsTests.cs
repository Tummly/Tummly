using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestTaggingEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestTaggingEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateTag_IsIdempotent_ByNormalizedName()
        {
            var owner = await SeedOwnerAsync("guest-tag-create-token-12345");

            using var first = new HttpRequestMessage(
                HttpMethod.Post,
                TagsUrl(owner.LocationId)
            );
            first.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            first.Content = JsonContent.Create(new { name = "  VIP  Guest " });

            var firstResponse = await _client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            var firstBody = await ReadJsonAsync(firstResponse);
            Assert.Equal("VIP Guest", firstBody.GetProperty("tag").GetProperty("name").GetString());
            var tagId = firstBody.GetProperty("tag").GetProperty("id").GetInt32();

            using var second = new HttpRequestMessage(
                HttpMethod.Post,
                TagsUrl(owner.LocationId)
            );
            second.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            second.Content = JsonContent.Create(new { name = "vip guest" });

            var secondResponse = await _client.SendAsync(second);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            var secondBody = await ReadJsonAsync(secondResponse);
            Assert.Equal(
                tagId,
                secondBody.GetProperty("tag").GetProperty("id").GetInt32()
            );
            Assert.Equal(
                "VIP Guest",
                secondBody.GetProperty("tag").GetProperty("name").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.GuestTags.CountAsync(t =>
                    t.RestaurantId == owner.RestaurantId
                )
            );
        }

        [Fact]
        public async Task ApplyTags_IsAdditiveOnly_AndPickerCountsLocationScope()
        {
            var seeded = await SeedGuestsWithTagsAsync(
                "guest-tag-apply-token-123456"
            );

            using var apply = new HttpRequestMessage(
                HttpMethod.Post,
                ApplyUrl(seeded.LocationId)
            );
            apply.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            apply.Content = JsonContent.Create(new
            {
                guestIds = new[] { seeded.GuestAId, seeded.GuestBId },
                tagIds = new[] { seeded.TagVipId, seeded.TagRegularId },
            });

            var applyResponse = await _client.SendAsync(apply);
            Assert.Equal(HttpStatusCode.OK, applyResponse.StatusCode);

            // Re-apply same tags — still OK, no duplicate memberships.
            using var reapply = new HttpRequestMessage(
                HttpMethod.Post,
                ApplyUrl(seeded.LocationId)
            );
            reapply.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            reapply.Content = JsonContent.Create(new
            {
                guestIds = new[] { seeded.GuestAId },
                tagIds = new[] { seeded.TagVipId },
            });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(reapply)).StatusCode
            );

            using var list = new HttpRequestMessage(
                HttpMethod.Get,
                TagsUrl(seeded.LocationId)
            );
            list.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var listResponse = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listBody = await ReadJsonAsync(listResponse);
            var tags = listBody.GetProperty("tags").EnumerateArray().ToList();

            var vip = tags.Single(t =>
                t.GetProperty("id").GetInt32() == seeded.TagVipId
            );
            var regular = tags.Single(t =>
                t.GetProperty("id").GetInt32() == seeded.TagRegularId
            );

            Assert.Equal(2, vip.GetProperty("guestCount").GetInt32());
            Assert.Equal(2, regular.GetProperty("guestCount").GetInt32());

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                Assert.Equal(
                    4,
                    await context.LocationGuestTags.CountAsync(m =>
                        m.GuestTag!.RestaurantId == seeded.RestaurantId
                    )
                );
            }
        }

        [Fact]
        public async Task ListMemberships_ReturnsTagIdsPerGuest()
        {
            var seeded = await SeedGuestsWithTagsAsync(
                "guest-tag-memberships-token-12"
            );

            using var apply = new HttpRequestMessage(
                HttpMethod.Post,
                ApplyUrl(seeded.LocationId)
            );
            apply.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            apply.Content = JsonContent.Create(new
            {
                guestIds = new[] { seeded.GuestAId },
                tagIds = new[] { seeded.TagVipId, seeded.TagRegularId },
            });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(apply)).StatusCode
            );

            using var list = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/guests/tags/memberships?locationId={seeded.LocationId}"
                    + $"&guestIds={seeded.GuestAId}"
                    + $"&guestIds={seeded.GuestBId}"
            );
            list.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var memberships = body.GetProperty("memberships")
                .EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("guestId").GetInt32(),
                    row =>
                        row.GetProperty("tagIds")
                            .EnumerateArray()
                            .Select(id => id.GetInt32())
                            .OrderBy(id => id)
                            .ToArray()
                );

            Assert.Equal(
                new[] { seeded.TagRegularId, seeded.TagVipId }.OrderBy(id => id),
                memberships[seeded.GuestAId]
            );
            Assert.Empty(memberships[seeded.GuestBId]);
        }

        [Fact]
        public async Task EnsureFromDetectedTag_OperatorCreatedWins()
        {
            var owner = await SeedOwnerAsync("guest-tag-ai-wins-token-1234");

            using var scope = _factory.Services.CreateScope();
            var tagging = scope.ServiceProvider
                .GetRequiredService<IGuestTaggingService>();

            var operatorTag = await tagging.CreateByNameAsync(
                owner.RestaurantId,
                "Food quality"
            );
            Assert.False(operatorTag.AiSourced);
            Assert.Null(operatorTag.DetectedTagKey);

            var ensured = await tagging.EnsureFromDetectedTagAsync(
                owner.RestaurantId,
                DetectedTag.FoodQuality
            );

            Assert.Equal(operatorTag.Id, ensured.Id);
            Assert.False(ensured.AiSourced);
            Assert.Null(ensured.DetectedTagKey);
            Assert.Equal("Food quality", ensured.DisplayName);

            var aiOnly = await tagging.EnsureFromDetectedTagAsync(
                owner.RestaurantId,
                DetectedTag.WaitTime
            );
            Assert.True(aiOnly.AiSourced);
            Assert.Equal("WaitTime", aiOnly.DetectedTagKey);
            Assert.Equal("Wait time", aiOnly.DisplayName);
            Assert.Equal(
                DetectedTagLabels.For(DetectedTag.WaitTime),
                aiOnly.DisplayName
            );
        }

        [Fact]
        public async Task UnionFromFeedback_CreatesMembership_Idempotently()
        {
            var seeded = await SeedSucceededFeedbackAsync(
                "guest-tag-union-token-123456",
                DetectedTag.Service,
                DetectedTag.Cleanliness
            );

            using var scope = _factory.Services.CreateScope();
            var tagging = scope.ServiceProvider
                .GetRequiredService<IGuestTaggingService>();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var feedback = await context.Feedbacks
                .SingleAsync(f => f.Id == seeded.FeedbackId);

            await tagging.UnionDetectedTagsFromFeedbackAsync(feedback);
            await tagging.UnionDetectedTagsFromFeedbackAsync(feedback);

            var memberships = await context.LocationGuestTags
                .Where(m => m.LocationGuestId == seeded.LocationGuestId)
                .Include(m => m.GuestTag)
                .ToListAsync();

            Assert.Equal(2, memberships.Count);
            Assert.All(memberships, m => Assert.True(m.GuestTag!.AiSourced));
            Assert.Contains(
                memberships,
                m => m.GuestTag!.DetectedTagKey == "Service"
            );
            Assert.Contains(
                memberships,
                m => m.GuestTag!.DetectedTagKey == "Cleanliness"
            );
        }

        [Fact]
        public async Task Backfill_UnionsSucceededFeedbacks_SkipsNullGuest()
        {
            var seeded = await SeedSucceededFeedbackAsync(
                "guest-tag-backfill-token-1234",
                DetectedTag.Billing
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = null,
                        GuestName = "Unlinked",
                        GuestContact = "unlinked@example.com",
                        ContactType = ContactType.Email,
                        Comment = "No guest",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Neutral,
                        DetectedTagsJson =
                            FeedbackClassificationMapping.SerializeDetectedTags(
                                new[] { DetectedTag.Atmosphere }
                            ),
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var backfill = scope.ServiceProvider
                    .GetRequiredService<IGuestTagBackfillService>();
                await backfill.BackfillAsync();
                await backfill.BackfillAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var memberships = await context.LocationGuestTags
                    .Include(m => m.GuestTag)
                    .Where(m => m.LocationGuestId == seeded.LocationGuestId)
                    .ToListAsync();

                Assert.Single(memberships);
                Assert.Equal("Billing", memberships[0].GuestTag!.DetectedTagKey);

                Assert.False(
                    await context.GuestTags.AnyAsync(t =>
                        t.RestaurantId == seeded.RestaurantId
                        && t.DetectedTagKey == "Atmosphere"
                    )
                );
            }
        }

        [Fact]
        public async Task CorrectSentiment_ReUnionsDetectedTagsOntoGuest()
        {
            var seeded = await SeedSucceededFeedbackAsync(
                "guest-tag-correct-reun-token12",
                DetectedTag.Value
            );

            // Simulate guest missed at succeed time: no memberships yet.
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                Assert.Empty(
                    await context.LocationGuestTags
                        .Where(m => m.LocationGuestId == seeded.LocationGuestId)
                        .ToListAsync()
                );
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/feedback/{seeded.FeedbackId}/classification"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            request.Content = JsonContent.Create(new { sentiment = "neutral" });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var memberships = await context.LocationGuestTags
                    .Include(m => m.GuestTag)
                    .Where(m => m.LocationGuestId == seeded.LocationGuestId)
                    .ToListAsync();

                Assert.Single(memberships);
                Assert.Equal("Value", memberships[0].GuestTag!.DetectedTagKey);
            }
        }

        [Fact]
        public async Task ListTags_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/guests/tags?locationId=1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId
        )> SeedOwnerAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Tag Owner",
                Email = $"{linkToken}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
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
                Name = "Tag Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = linkToken,
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

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int GuestAId,
            int GuestBId,
            int TagVipId,
            int TagRegularId
        )> SeedGuestsWithTagsAsync(string linkToken)
        {
            var owner = await SeedOwnerAsync(linkToken);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var tagging = scope.ServiceProvider
                .GetRequiredService<IGuestTaggingService>();

            async Task<int> AddGuestAsync(string name, string email)
            {
                var master = new MasterGuest
                {
                    RestaurantId = owner.RestaurantId,
                    Email = email,
                    NormalizedEmail = email,
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var guest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = owner.LocationId,
                    Name = name,
                    CreatedAt = DateTime.UtcNow,
                };
                context.LocationGuests.Add(guest);
                await context.SaveChangesAsync();
                return guest.Id;
            }

            var guestA = await AddGuestAsync("A", "a@example.com");
            var guestB = await AddGuestAsync("B", "b@example.com");
            var vip = await tagging.CreateByNameAsync(owner.RestaurantId, "VIP");
            var regular = await tagging.CreateByNameAsync(
                owner.RestaurantId,
                "Regular"
            );

            return (
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                guestA,
                guestB,
                vip.Id,
                regular.Id
            );
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int LocationGuestId,
            int FeedbackId
        )> SeedSucceededFeedbackAsync(
            string linkToken,
            params DetectedTag[] tags
        )
        {
            var owner = await SeedOwnerAsync(linkToken);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = owner.RestaurantId,
                Email = "guest@example.com",
                NormalizedEmail = "guest@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = owner.LocationId,
                Name = "Tagged Guest",
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = owner.LocationId,
                LocationGuestId = locationGuest.Id,
                GuestName = "Tagged Guest",
                GuestContact = "guest@example.com",
                ContactType = ContactType.Email,
                Comment = "Nice visit",
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Positive,
                DetectedTagsJson =
                    FeedbackClassificationMapping.SerializeDetectedTags(tags),
                CreatedAt = DateTime.UtcNow,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            return (
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                locationGuest.Id,
                feedback.Id
            );
        }

        private static string TagsUrl(int locationId)
            => $"/api/guests/tags?locationId={locationId}";

        private static string ApplyUrl(int locationId)
            => $"/api/guests/tags/apply?locationId={locationId}";

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }
    }
}

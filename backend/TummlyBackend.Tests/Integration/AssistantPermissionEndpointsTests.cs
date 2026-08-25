using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class AssistantPermissionEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AssistantPermissionEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendTurn_Returns403_WhenAiAssistantIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(PermissionRoles.Staff);

            var response = await PostTurnAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "Summarise recent feedback"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SendTurn_Returns403_WhenLocationOutsideScope()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing
            );

            var response = await PostTurnAsync(
                seeded.MemberJwt,
                seeded.OutOfScopeLocationId,
                "Summarise recent feedback"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Transcribe_Returns403_WhenAiAssistantIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(PermissionRoles.Staff);
            FakeStt.Reset();

            var response = await PostAudioAsync(seeded.MemberJwt);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(0, FakeStt.TranscribeCallCount);
        }

        [Fact]
        public async Task SendTurn_ViewRole_DoesNotPersistCampaignDraft()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.ReportingOnly
            );
            ResetFake();

            var response = await PostTurnAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "Create a campaign with 10% off valid 30 days after issue at Camden"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_DoesNotLoadFeedback_WhenFeedbackIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(
                PermissionRoles.Marketing
            );
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.InScopeLocationId,
                        GuestName = "Secret Feedback Guest",
                        GuestContact = "secret@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Unique secret comment xyzzy",
                        OffersOptOut = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        DetectedTagsJson = "[\"Service\"]",
                        WorkflowStatus = FeedbackWorkflowStatus.New,
                    }
                );
                await context.SaveChangesAsync();
            }
            ResetFake();

            var response = await PostTurnAsync(
                seeded.MemberJwt,
                seeded.InScopeLocationId,
                "Summarise recent feedback"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            var conversation = body.GetProperty("conversation");
            var json = conversation.GetRawText();
            Assert.DoesNotContain("Secret Feedback Guest", json);
            Assert.DoesNotContain("Unique secret comment xyzzy", json);
        }

        [Fact]
        public async Task RestaurantLocations_HidesAssistant_WhenAiAssistantIsNoAccess()
        {
            var seeded = await SeedOwnerAndMemberAsync(PermissionRoles.Staff);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/restaurant/locations"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("aiAssistantAccess").GetBoolean());
        }

        private FakeSpeechToTextProvider FakeStt =>
            _factory.Services.GetRequiredService<FakeSpeechToTextProvider>();

        private void ResetFake()
        {
            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeAssistantLiveAnswerProvider>();
            fake.ResetToCannedStub();
        }

        private async Task<HttpResponseMessage> PostTurnAsync(
            string jwt,
            int locationId,
            string message
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(new
            {
                message,
                analysisScope = new
                {
                    ownedLocationId = locationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PostAudioAsync(string jwt)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(
                Encoding.UTF8.GetBytes("fake-webm-audio-bytes")
            );
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/webm");
            content.Add(fileContent, "audio", "clip.webm");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/stt"
            )
            {
                Content = content,
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return await _client.SendAsync(request);
        }

        private async Task<PermissionSeed> SeedOwnerAndMemberAsync(
            string memberRole
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Assistant Scope Owner",
                Email = $"owner-14-{Guid.NewGuid():N}@example.com",
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
                Name = "Assistant Scope Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
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
                FullName = "Assistant Scope Member",
                Email = $"member-14-{Guid.NewGuid():N}@example.com",
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
                PermissionRole = memberRole,
                LocationScope = LocationScopeKind.NamedList,
                NamedLocationIdsJson =
                    MembershipLocationScope.SerializeNamedIds([inScope.Id]),
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            return new PermissionSeed(
                jwtService.GenerateToken(
                    member.Id.ToString(),
                    member.Email,
                    member.Role
                ),
                inScope.Id,
                outOfScope.Id
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record PermissionSeed(
            string MemberJwt,
            int InScopeLocationId,
            int OutOfScopeLocationId
        );
    }
}

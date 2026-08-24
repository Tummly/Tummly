using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class HelpCentreCreateEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HelpCentreCreateEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostQuery_ContactUs_CreatesKindNullQuery()
        {
            var response = await PostContactQueryAsync(
                topic: "billing",
                message: "Need help with credits."
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadDataAsync(response);
            Assert.True(body.GetProperty("id").GetInt32() > 0);
            Assert.Equal("NEW", body.GetProperty("status").GetString());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var query = await context.HelpCentreQueries
                .Where(q => q.AccountRequestKind == null)
                .OrderByDescending(q => q.Id)
                .FirstAsync();
            Assert.Null(query.AccountRequestKind);
            Assert.Null(query.RestaurantId);
            Assert.Equal(HelpCentreQueryTopic.Billing, query.Topic);
        }

        [Theory]
        [InlineData(
            "TransferOwnership",
            "something-else",
            "Ownership transfer requested from Account controls."
        )]
        [InlineData(
            "AccountExport",
            "privacy-data",
            "Account export requested from Account controls."
        )]
        [InlineData(
            "AccountClosure",
            "privacy-data",
            "Account closure requested from Account controls."
        )]
        public async Task PostQuery_AccountRequest_CreatesQueryWithKindTopicAndStatus(
            string kind,
            string expectedTopicSlug,
            string closingLine
        )
        {
            var tracking = new TrackingHelpCentreEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerAsync();

            var response = await PostAccountRequestAsync(
                client,
                seeded.Jwt,
                seeded.RestaurantId,
                kind,
                seeded.Email
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadDataAsync(response);
            Assert.Equal("NEW", body.GetProperty("status").GetString());
            Assert.True(body.GetProperty("emailDispatched").GetBoolean());
            Assert.Equal(1, tracking.NewQueryEmailCalls);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var query = await context.HelpCentreQueries
                .Include(q => q.Messages)
                .SingleAsync(q => q.RestaurantId == seeded.RestaurantId);

            Assert.Equal(
                HelpCentreAccountRequestKindExtensions.FromWireString(kind),
                query.AccountRequestKind
            );
            Assert.Equal(seeded.RestaurantId, query.RestaurantId);
            Assert.Null(query.RestaurantLocationId);
            Assert.Equal(
                HelpCentreQueryTopicExtensions.FromSlug(expectedTopicSlug),
                query.Topic
            );
            Assert.Equal(HelpCentreQueryStatus.New, query.Status);
            Assert.Equal(seeded.OwnerUserId, query.UserId);

            var message = query.Messages.Single().Body;
            Assert.Contains("Account request kind:", message);
            Assert.Contains("Account Workspace Venue", message);
            Assert.Contains($"Restaurant id: {seeded.RestaurantId}", message);
            Assert.Contains(seeded.Email, message);
            Assert.Contains(closingLine, message);
        }

        [Fact]
        public async Task PostQuery_AccountRequest_ReturnsEmailWarning_WhenEmailFails()
        {
            var tracking = new TrackingHelpCentreEmailService
            {
                ThrowOnNewQuery = true,
            };
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerAsync(email: "hc-email-fail@example.com");

            var response = await PostAccountRequestAsync(
                client,
                seeded.Jwt,
                seeded.RestaurantId,
                "TransferOwnership"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadDataAsync(response);
            Assert.False(body.GetProperty("emailDispatched").GetBoolean());
            Assert.Equal(
                EmailDispatch.DefaultWarning,
                body.GetProperty("emailWarning").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Single(
                await context.HelpCentreQueries.Where(
                    q => q.RestaurantId == seeded.RestaurantId
                ).ToListAsync()
            );
        }

        [Fact]
        public async Task PostQuery_AccountRequest_BlocksDuplicateOpenRequest()
        {
            var seeded = await SeedOwnerAsync(
                email: "hc-dup-open@example.com"
            );
            await SeedOpenAccountRequestAsync(
                seeded.RestaurantId,
                seeded.OwnerUserId,
                HelpCentreAccountRequestKind.TransferOwnership,
                HelpCentreQueryStatus.InProgress
            );

            var client = CreateClientWithEmail(new TrackingHelpCentreEmailService());
            var response = await PostAccountRequestAsync(
                client,
                seeded.Jwt,
                seeded.RestaurantId,
                "TransferOwnership"
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.TryGetProperty("existingQueryId", out _));

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.HelpCentreQueries.CountAsync(
                    q =>
                        q.RestaurantId == seeded.RestaurantId
                        && q.AccountRequestKind
                            == HelpCentreAccountRequestKind.TransferOwnership
                )
            );
        }

        [Fact]
        public async Task PostQuery_AccountRequest_AllowsNewRequestAfterResolved()
        {
            var seeded = await SeedOwnerAsync(
                email: "hc-resolved@example.com"
            );
            await SeedOpenAccountRequestAsync(
                seeded.RestaurantId,
                seeded.OwnerUserId,
                HelpCentreAccountRequestKind.AccountExport,
                HelpCentreQueryStatus.Resolved
            );

            var client = CreateClientWithEmail(new TrackingHelpCentreEmailService());
            var response = await PostAccountRequestAsync(
                client,
                seeded.Jwt,
                seeded.RestaurantId,
                "AccountExport"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                2,
                await context.HelpCentreQueries.CountAsync(
                    q =>
                        q.RestaurantId == seeded.RestaurantId
                        && q.AccountRequestKind
                            == HelpCentreAccountRequestKind.AccountExport
                )
            );
        }

        [Fact]
        public async Task GetOpenAccountRequest_ReturnsExistingQueryId()
        {
            var seeded = await SeedOwnerAsync(email: "hc-open-get@example.com");
            var existing = await SeedOpenAccountRequestAsync(
                seeded.RestaurantId,
                seeded.OwnerUserId,
                HelpCentreAccountRequestKind.AccountClosure,
                HelpCentreQueryStatus.New
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/help-centre/account-requests/open?restaurantId={seeded.RestaurantId}&kind=AccountClosure"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadDataAsync(response);
            Assert.Equal(existing.Id, body.GetProperty("queryId").GetInt32());
        }

        private HttpClient CreateClientWithEmail(
            TrackingHelpCentreEmailService emailService
        )
        {
            return _factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var descriptors = services
                            .Where(d => d.ServiceType == typeof(IEmailService))
                            .ToList();
                        foreach (var descriptor in descriptors)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddSingleton<IEmailService>(emailService);
                    });
                })
                .CreateClient();
        }

        private async Task<HttpResponseMessage> PostContactQueryAsync(
            string topic,
            string message
        )
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(topic), "topic");
            content.Add(new StringContent("Test Cafe"), "businessName");
            content.Add(new StringContent("Jane"), "submitterName");
            content.Add(new StringContent("jane@example.com"), "submitterEmail");
            content.Add(new StringContent(message), "message");

            return await _client.PostAsync("/api/help-centre/queries", content);
        }

        private static async Task<HttpResponseMessage> PostAccountRequestAsync(
            HttpClient client,
            string jwt,
            int restaurantId,
            string kind,
            string submitterEmail = "owner@example.com"
        )
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(kind), "accountRequestKind");
            content.Add(new StringContent(restaurantId.ToString()), "restaurantId");
            content.Add(new StringContent("Account Workspace Venue"), "businessName");
            content.Add(new StringContent("Account Workspace Owner"), "submitterName");
            content.Add(new StringContent(submitterEmail), "submitterEmail");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/help-centre/queries"
            )
            {
                Content = content,
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            return await client.SendAsync(request);
        }

        private async Task<HelpCentreQuery> SeedOpenAccountRequestAsync(
            int restaurantId,
            int ownerUserId,
            HelpCentreAccountRequestKind kind,
            HelpCentreQueryStatus status
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var query = new HelpCentreQuery
            {
                Topic = HelpCentreAccountRequestKindExtensions.TopicForKind(kind),
                SubmitterName = "Owner",
                SubmitterEmail = "owner@example.com",
                BusinessName = "Venue",
                UserId = ownerUserId,
                RestaurantId = restaurantId,
                AccountRequestKind = kind,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Messages =
                [
                    new HelpCentreQueryMessage
                    {
                        AuthorKind = HelpCentreQueryAuthorKind.Submitter,
                        AuthorUserId = ownerUserId,
                        Body = "Existing request",
                        CreatedAt = DateTime.UtcNow,
                    },
                ],
            };

            context.HelpCentreQueries.Add(query);
            await context.SaveChangesAsync();
            return query;
        }

        private async Task<(
            string Jwt,
            int RestaurantId,
            int OwnerUserId,
            string Email
        )> SeedOwnerAsync(string email = "hc-owner@example.com")
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Account Workspace Owner",
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
                Name = "Account Workspace Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                BillingContactUserId = user.Id,
                PrivacyContactUserId = user.Id,
                SupportContactUserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, restaurant.Id, user.Id, user.Email);
        }

        private static async Task<JsonElement> ReadDataAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.GetProperty("data");
        }

        private sealed class TrackingHelpCentreEmailService : EmailServiceStubBase
        {
            public int NewQueryEmailCalls { get; private set; }

            public bool ThrowOnNewQuery { get; set; }

            public override Task SendHelpCentreNewQueryEmailAsync(
                string topicLabel,
                string submitterName,
                string submitterEmail,
                string businessName,
                string? locationLabel,
                string messagePreview,
                int attachmentCount,
                string supportDashboardUrl
            )
            {
                NewQueryEmailCalls++;

                if (ThrowOnNewQuery)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }
        }
    }
}

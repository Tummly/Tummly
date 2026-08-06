using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackGuestPreviewSendTestEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public FeedbackGuestPreviewSendTestEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
        }

        [Fact]
        public async Task SendGuestPreviewTest_SendsToOperator_CreatesNoGuestResponseFact()
        {
            var tracking = new TrackingGuestResponseEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-preview-send-test-tok",
                guestContact: "guest-inbox@example.com",
                email: "operator-send-test@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-preview-send-test"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                subject = "Thanks for visiting",
                body = "Hi guest, thanks for your feedback.",
            });

            var response = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, tracking.CallCount);
            Assert.Equal(seeded.OperatorEmail, tracking.LastToEmail);
            Assert.DoesNotContain(
                "guest-inbox@example.com",
                tracking.LastToEmail ?? ""
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.FeedbackGuestResponses.CountAsync(
                    r => r.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task SendGuestPreviewTest_Returns502_WhenResendFails()
        {
            var tracking = new TrackingGuestResponseEmailService
            {
                ThrowOnSend = true,
            };
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-preview-send-test-fail-tok",
                guestContact: "guest-fail@example.com",
                email: "operator-fail@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-preview-send-test"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                subject = "Subject",
                body = "Body text",
            });

            var response = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
        }

        [Fact]
        public async Task SendGuestPreviewTest_Returns403_ForNonOwner()
        {
            var tracking = new TrackingGuestResponseEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-preview-send-test-cross-tok",
                guestContact: "cross@example.com",
                email: "cross-owner@example.com"
            );
            var otherJwt = await SeedOtherOwnerJwtAsync(
                "guest-preview-send-test-cross-other-tok"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-preview-send-test"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", otherJwt);
            post.Content = JsonContent.Create(new
            {
                subject = "Subject",
                body = "Body text",
            });

            var response = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(0, tracking.CallCount);
        }

        private HttpClient CreateClientWithEmail(
            TrackingGuestResponseEmailService tracking
        )
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var existing = services
                        .Where(d => d.ServiceType == typeof(IEmailService))
                        .ToList();
                    foreach (var descriptor in existing)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<IEmailService>(tracking);
                });
            }).CreateClient();
        }

        private async Task<(
            string Jwt,
            int FeedbackId,
            string OperatorEmail
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            string guestContact,
            string email
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var operatorEmail = email + "-" + linkToken;
            var user = new User
            {
                FullName = "Preview Send Test Owner",
                Email = operatorEmail,
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
                Name = "Preview Send Test Venue",
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

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[\"FoodQuality\"]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, feedback.Id, operatorEmail);
        }

        private async Task<string> SeedOtherOwnerJwtAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Other Preview Owner",
                Email = $"other-preview-owner-{linkToken}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900456",
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
                Name = "Other Preview Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }

        private sealed class TrackingGuestResponseEmailService : EmailServiceStubBase
        {
            public int CallCount { get; private set; }

            public string? LastToEmail { get; private set; }

            public bool ThrowOnSend { get; set; }

            public override Task SendGuestResponseEmailAsync(
                string toEmail,
                string subject,
                string brandTitle,
                string? brandSubtitle,
                string? locationAddress,
                string message,
                string giveFeedbackUrl,
                string? brandLogoUrl = null,
                GuestResponseEmailOfferBlock? offer = null
            )
            {
                CallCount++;
                LastToEmail = toEmail;

                if (ThrowOnSend)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }
        }
    }
}

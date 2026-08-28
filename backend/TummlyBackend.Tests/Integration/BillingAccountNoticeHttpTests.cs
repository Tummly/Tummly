using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class BillingAccountNoticeHttpTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BillingAccountNoticeHttpTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreditWarning_ExpandedTicksReceiveEmailAndInboxViaHttp()
        {
            var seeded = await SeedAndNotifyCreditWarningAsync(
                accountNoticesOff: false
            );

            Assert.Equal(2, seeded.EmailService.Sent.Count);

            using var ownerRequest = AuthorizedGet(
                "/api/notifications",
                seeded.OwnerJwt
            );
            var ownerResponse = await _client.SendAsync(ownerRequest);
            Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
            var ownerBody = await ReadJsonAsync(ownerResponse);
            var ownerNotice = ownerBody
                .GetProperty("items")
                .EnumerateArray()
                .Single();
            Assert.Equal(
                "credit-warning-90",
                ownerNotice.GetProperty("type").GetString()
            );
            Assert.Equal(
                "View usage",
                ownerNotice.GetProperty("ctaLabel").GetString()
            );
            Assert.Contains(
                "Harbour Kitchen",
                ownerNotice.GetProperty("body").GetString()
            );
        }

        [Fact]
        public async Task CreditWarning_AccountNoticesOff_MutesInboxOnlyViaHttp()
        {
            var seeded = await SeedAndNotifyCreditWarningAsync(
                accountNoticesOff: true
            );

            Assert.Equal(2, seeded.EmailService.Sent.Count);

            using var ownerRequest = AuthorizedGet(
                "/api/notifications",
                seeded.OwnerJwt
            );
            var ownerResponse = await _client.SendAsync(ownerRequest);
            var ownerBody = await ReadJsonAsync(ownerResponse);
            Assert.Equal(0, ownerBody.GetProperty("items").GetArrayLength());

            using var adminRequest = AuthorizedGet(
                "/api/notifications",
                seeded.AdminJwt
            );
            var adminResponse = await _client.SendAsync(adminRequest);
            var adminBody = await ReadJsonAsync(adminResponse);
            Assert.Equal(1, adminBody.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task CreditWarning_NoAccessRecipient_EmailWithoutCtaViaHttp()
        {
            var seeded = await SeedStaffOnlyRecipientAsync();

            using var staffRequest = AuthorizedGet(
                "/api/notifications",
                seeded.StaffJwt
            );
            var staffResponse = await _client.SendAsync(staffRequest);
            var staffBody = await ReadJsonAsync(staffResponse);
            var notice = staffBody.GetProperty("items").EnumerateArray().Single();
            Assert.Equal(
                JsonValueKind.Null,
                notice.GetProperty("ctaLabel").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                notice.GetProperty("ctaHref").ValueKind
            );

            var staffEmail = Assert.Single(seeded.EmailService.Sent);
            Assert.Null(staffEmail.CtaLabel);
            Assert.Null(staffEmail.CtaHref);
        }

        private async Task<NotifySeed> SeedAndNotifyCreditWarningAsync(
            bool accountNoticesOff
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            var emailService = new TrackingBillingEmailService();

            var owner = AddUser(context, "Owner", "owner@example.com");
            var admin = AddUser(context, "Admin", "admin@example.com");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Harbour Kitchen",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = admin.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    LowCreditAlertOwner = true,
                    LowCreditAlertAdmin = true,
                    LowCreditAlertBillingContact = false,
                    PaymentFailureAlertOwner = true,
                    PaymentFailureAlertBillingContact = true,
                }
            );
            AddMembership(
                context,
                owner.Id,
                restaurant.Id,
                PermissionRoles.Owner
            );
            AddMembership(
                context,
                admin.Id,
                restaurant.Id,
                PermissionRoles.Admin
            );
            owner.SelectedRestaurantId = restaurant.Id;
            admin.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            var notifications = new OperatorNotificationsService(
                context,
                new NullNotificationRealtimePublisher()
            );
            if (accountNoticesOff)
            {
                await notifications.SetPreferencesAsync(
                    owner.Id,
                    new NotificationPreferencesDto { AccountNotices = false }
                );
            }

            var notifier = new BillingAccountNoticeNotifier(
                context,
                notifications,
                emailService,
                PricebookCatalog.LoadFromDirectory(
                Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "billing-pack-v3.0"
                    )
                )
            )
            );
            await notifier.NotifyCreditThresholdCrossedAsync(
                restaurant.Id,
                "email",
                90,
                "period-1",
                "Past due",
                isPilot: true
            );

            return new NotifySeed(
                emailService,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                ),
                jwtService.GenerateToken(
                    admin.Id.ToString(),
                    admin.Email,
                    admin.Role
                )
            );
        }

        private async Task<StaffNotifySeed> SeedStaffOnlyRecipientAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            var emailService = new TrackingBillingEmailService();

            var owner = AddUser(context, "Owner", "owner2@example.com");
            var staff = AddUser(context, "Staff", "staff@example.com");
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Harbour Kitchen",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = staff.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.RestaurantLocations.Add(
                new RestaurantLocation
                {
                    RestaurantId = restaurant.Id,
                    LocationName = "Main",
                    Address = "1 High Street",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    LowCreditAlertOwner = false,
                    LowCreditAlertAdmin = false,
                    LowCreditAlertBillingContact = true,
                    PaymentFailureAlertOwner = true,
                    PaymentFailureAlertBillingContact = true,
                }
            );
            AddMembership(
                context,
                owner.Id,
                restaurant.Id,
                PermissionRoles.Owner
            );
            AddMembership(
                context,
                staff.Id,
                restaurant.Id,
                PermissionRoles.Staff
            );
            staff.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            var notifier = new BillingAccountNoticeNotifier(
                context,
                new OperatorNotificationsService(
                    context,
                    new NullNotificationRealtimePublisher()
                ),
                emailService,
                PricebookCatalog.LoadFromDirectory(
                    Path.GetFullPath(
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "..",
                            "..",
                            "..",
                            "..",
                            "..",
                            "docs",
                            "product",
                            "billing-pack-v3.0"
                        )
                    )
                )
            );
            await notifier.NotifyCreditThresholdCrossedAsync(
                restaurant.Id,
                "sms",
                80,
                "period-1",
                "Active",
                isPilot: true
            );

            return new StaffNotifySeed(
                emailService,
                jwtService.GenerateToken(
                    staff.Id.ToString(),
                    staff.Email,
                    staff.Role
                )
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            return doc.RootElement.Clone();
        }

        private static HttpRequestMessage AuthorizedGet(string path, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static User AddUser(
            ApplicationDbContext context,
            string fullName,
            string email
        )
        {
            var user = new User
            {
                Email = email,
                PasswordHash = "x",
                FullName = fullName,
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            return user;
        }

        private static void AddMembership(
            ApplicationDbContext context,
            int userId,
            int restaurantId,
            string role
        )
        {
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = userId,
                    RestaurantId = restaurantId,
                    PermissionRole = role,
                    Status = MembershipStatus.Active,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                }
            );
        }

        private sealed record NotifySeed(
            TrackingBillingEmailService EmailService,
            string OwnerJwt,
            string AdminJwt
        );

        private sealed record StaffNotifySeed(
            TrackingBillingEmailService EmailService,
            string StaffJwt
        );

        private sealed class TrackingBillingEmailService : EmailServiceStubBase
        {
            public List<SentBillingEmail> Sent { get; } = [];

            public override Task SendBillingAccountNoticeEmailAsync(
                string toEmail,
                string firstName,
                string title,
                string body,
                string? ctaLabel,
                string? ctaHref
            )
            {
                Sent.Add(
                    new SentBillingEmail(toEmail, ctaLabel, ctaHref)
                );
                return Task.CompletedTask;
            }
        }

        private sealed record SentBillingEmail(
            string ToEmail,
            string? CtaLabel,
            string? CtaHref
        );
    }
}

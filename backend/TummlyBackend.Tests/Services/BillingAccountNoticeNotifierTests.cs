using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class BillingAccountNoticeNotifierTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OperatorNotificationsService _notifications;
        private readonly TrackingBillingEmailService _email;
        private readonly BillingAccountNoticeNotifier _notifier;

        public BillingAccountNoticeNotifierTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _notifications = new OperatorNotificationsService(
                _context,
                new NullNotificationRealtimePublisher()
            );
            _email = new TrackingBillingEmailService();
            _notifier = new BillingAccountNoticeNotifier(
                _context,
                _notifications,
                _email
            );
        }

        [Fact]
        public async Task NotifyCreditThresholdCrossed_SendsEmailToExpandedTicks()
        {
            var seed = await SeedWorkspaceAsync();

            await _notifier.NotifyCreditThresholdCrossedAsync(
                seed.RestaurantId,
                "email",
                80,
                "pilot-once",
                "Past due",
                isPilot: true
            );

            Assert.Equal(2, _email.Sent.Count);
            Assert.Contains(_email.Sent, row => row.ToEmail == seed.OwnerEmail);
            Assert.Contains(_email.Sent, row => row.ToEmail == seed.AdminEmail);
            Assert.DoesNotContain(
                _email.Sent,
                row => row.ToEmail == seed.StaffEmail
            );

            var ownerInbox = await _notifications.ListAsync(seed.OwnerUserId);
            var adminInbox = await _notifications.ListAsync(seed.AdminUserId);
            Assert.Single(ownerInbox);
            Assert.Single(adminInbox);
            Assert.Equal("credit-warning-80", ownerInbox[0].Type);
        }

        [Fact]
        public async Task NotifyCreditThresholdCrossed_AccountNoticesOff_MutesInboxOnly()
        {
            var seed = await SeedWorkspaceAsync();
            await _notifications.SetPreferencesAsync(
                seed.OwnerUserId,
                new NotificationPreferencesDto { AccountNotices = false }
            );

            await _notifier.NotifyCreditThresholdCrossedAsync(
                seed.RestaurantId,
                "sms",
                90,
                "pilot-once",
                "Active",
                isPilot: true
            );

            Assert.Equal(2, _email.Sent.Count);
            Assert.Empty(await _notifications.ListAsync(seed.OwnerUserId));
            Assert.Single(await _notifications.ListAsync(seed.AdminUserId));
        }

        [Fact]
        public async Task NotifyCreditThresholdCrossed_NoAccessRecipient_EmailWithoutCta()
        {
            var seed = await SeedWorkspaceAsync(staffOnlyRecipient: true);

            await _notifier.NotifyCreditThresholdCrossedAsync(
                seed.RestaurantId,
                "ai",
                80,
                "pilot-once",
                "Active",
                isPilot: true
            );

            Assert.Single(_email.Sent);
            var staffEmail = Assert.Single(_email.Sent);
            Assert.Equal(seed.StaffEmail, staffEmail.ToEmail);
            Assert.Null(staffEmail.CtaLabel);
            Assert.Null(staffEmail.CtaHref);

            var staffInbox = await _notifications.ListAsync(seed.StaffUserId);
            var notice = Assert.Single(staffInbox);
            Assert.Null(notice.CtaLabel);
            Assert.Null(notice.CtaHref);
        }

        [Fact]
        public async Task NotifyCreditThresholdCrossed_SkipsDuringSoftLock()
        {
            var seed = await SeedWorkspaceAsync();

            await _notifier.NotifyCreditThresholdCrossedAsync(
                seed.RestaurantId,
                "email",
                100,
                "pilot-once",
                "Soft lock",
                isPilot: true
            );

            Assert.Empty(_email.Sent);
            Assert.Empty(_context.Notifications);
        }

        [Fact]
        public async Task NotifyCreditThresholdCrossed_SkipsDuringDormant()
        {
            var seed = await SeedWorkspaceAsync();

            await _notifier.NotifyCreditThresholdCrossedAsync(
                seed.RestaurantId,
                "email",
                80,
                "pilot-once",
                "Dormant",
                isPilot: true
            );

            Assert.Empty(_email.Sent);
            Assert.Empty(_context.Notifications);
        }

        [Fact]
        public async Task NotifyCreditThresholdCrossed_StillSendsDuringPastDue()
        {
            var seed = await SeedWorkspaceAsync();

            await _notifier.NotifyCreditThresholdCrossedAsync(
                seed.RestaurantId,
                "email",
                80,
                "pilot-once",
                "Past due",
                isPilot: true
            );

            Assert.Equal(2, _email.Sent.Count);
            Assert.Equal(2, await _context.Notifications.CountAsync());
        }

        [Fact]
        public async Task NotifyPaymentFailureDayStep_SendsToPaymentFailureTicks()
        {
            var seed = await SeedWorkspaceAsync();

            await _notifier.NotifyPaymentFailureDayStepAsync(
                seed.RestaurantId,
                7,
                "episode-1"
            );

            Assert.Equal(2, _email.Sent.Count);
            var ownerNotice = await _notifications.ListAsync(seed.OwnerUserId);
            Assert.Equal("payment-failure-day-7", ownerNotice[0].Type);
            Assert.Contains(
                "Campaign and recovery sends are blocked",
                ownerNotice[0].Body
            );
        }

        [Fact]
        public async Task NotifyUnpaidPilotLockEnter_SendsToOwnerAndBillingContact()
        {
            var seed = await SeedWorkspaceAsync();

            await _notifier.NotifyUnpaidPilotLockEnterAsync(
                seed.RestaurantId,
                "lock-1"
            );

            Assert.Equal(2, _email.Sent.Count);
            var types = await _context.Notifications
                .Select(row => row.Type)
                .Distinct()
                .ToListAsync();
            Assert.Equal(["unpaid-pilot-lock"], types);
        }


        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<Seed> SeedWorkspaceAsync(bool staffOnlyRecipient = false)
        {
            var owner = AddUser("Owner User", "owner@example.com");
            var admin = AddUser("Admin User", "admin@example.com");
            var staff = AddUser("Staff User", "staff@example.com");
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Harbour Kitchen",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = staffOnlyRecipient ? staff.Id : admin.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);

            _context.BillingAccounts.Add(
                new BillingAccount
                {
                    RestaurantId = restaurant.Id,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                    LowCreditAlertOwner = !staffOnlyRecipient,
                    LowCreditAlertAdmin = !staffOnlyRecipient,
                    LowCreditAlertBillingContact = staffOnlyRecipient,
                    PaymentFailureAlertOwner = true,
                    PaymentFailureAlertBillingContact = true,
                }
            );

            AddMembership(owner.Id, restaurant.Id, PermissionRoles.Owner);
            AddMembership(admin.Id, restaurant.Id, PermissionRoles.Admin);
            AddMembership(staff.Id, restaurant.Id, PermissionRoles.Staff);

            await _context.SaveChangesAsync();

            return new Seed(
                restaurant.Id,
                owner.Id,
                admin.Id,
                staff.Id,
                owner.Email,
                admin.Email,
                staff.Email
            );
        }

        private User AddUser(string fullName, string email)
        {
            var user = new User
            {
                Email = email,
                PasswordHash = "x",
                FullName = fullName,
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            return user;
        }

        private void AddMembership(int userId, int restaurantId, string role)
        {
            _context.RestaurantMemberships.Add(
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

        private sealed record Seed(
            int RestaurantId,
            int OwnerUserId,
            int AdminUserId,
            int StaffUserId,
            string OwnerEmail,
            string AdminEmail,
            string StaffEmail
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
                    new SentBillingEmail(
                        toEmail,
                        firstName,
                        title,
                        body,
                        ctaLabel,
                        ctaHref
                    )
                );
                return Task.CompletedTask;
            }
        }

        private sealed record SentBillingEmail(
            string ToEmail,
            string FirstName,
            string Title,
            string Body,
            string? CtaLabel,
            string? CtaHref
        );
    }
}

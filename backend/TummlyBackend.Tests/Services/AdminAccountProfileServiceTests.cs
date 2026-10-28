using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AdminAccountProfileServiceTests : IDisposable
    {
        private const string Pricebook = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly ApplicationDbContext _context;
        private readonly AdminAccountDirectoryService _service;

        public AdminAccountProfileServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = AdminAccountDirectoryServiceTests.CreateService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task Profile_UnknownWorkspace_ReturnsNull()
        {
            var profile = await _service.GetProfileAsync(int.MaxValue - 7);

            Assert.Null(profile);
        }

        [Fact]
        public async Task Profile_Identity_ShowsOwnerEmailInFull_AndWorkspaceIdAsBillingAccountId()
        {
            var id = SeedWorkspace("Identity Venue", ownerEmail: "owner.person@example.com", legal: "Identity Ltd");
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            Assert.NotNull(profile);
            Assert.Equal(id, profile!.Identity.RestaurantId);
            Assert.Equal("Identity Venue", profile.Identity.DisplayName);
            Assert.Equal("Identity Ltd", profile.Identity.LegalName);
            Assert.Equal("Owner Person", profile.Identity.OwnerName);
            Assert.Equal("owner.person@example.com", profile.Identity.OwnerEmail);
            Assert.Equal(id, profile.Identity.BillingAccountId);
        }

        [Fact]
        public async Task Profile_Lifecycle_ReturnsStoredServerFields()
        {
            var pilotStart = DateTime.UtcNow.AddDays(-5);
            var pilotEnd = DateTime.UtcNow.AddDays(9);
            var softLock = DateTime.UtcNow.AddDays(-1);
            var id = SeedWorkspace(
                "Lifecycle Venue",
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.SoftLock,
                pilotStart: pilotStart,
                pilotEnd: pilotEnd,
                softLockAt: softLock
            );
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            var lifecycle = profile!.Lifecycle;
            Assert.Equal(BillingSubscriptionPlans.Pilot, lifecycle.SubscriptionPlan);
            Assert.Equal(BillingStatuses.SoftLock, lifecycle.BillingStatus);
            Assert.Equal(pilotStart, lifecycle.PilotStartedAtUtc);
            Assert.Equal(pilotEnd, lifecycle.PilotPeriodEndUtc);
            Assert.Equal(softLock, lifecycle.SoftLockEnteredAtUtc);
            Assert.Null(lifecycle.DormantEnteredAtUtc);
            Assert.False(lifecycle.ScheduledChange.HasScheduledChange);
            Assert.Null(lifecycle.ScheduledChange.CancelAtUtc);
        }

        [Fact]
        public async Task Profile_PilotPastEndStillStoredPilot_FlagsNotAdvanced()
        {
            var id = SeedWorkspace(
                "Expired Pilot Venue",
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                pilotStart: DateTime.UtcNow.AddDays(-40),
                pilotEnd: DateTime.UtcNow.AddDays(-10)
            );
            var activeId = SeedWorkspace(
                "Active Pilot Venue",
                plan: BillingSubscriptionPlans.Pilot,
                status: BillingStatuses.Pilot,
                pilotStart: DateTime.UtcNow.AddDays(-5),
                pilotEnd: DateTime.UtcNow.AddDays(25)
            );
            await _context.SaveChangesAsync();

            var expired = (await _service.GetProfileAsync(id))!.Lifecycle;
            var active = (await _service.GetProfileAsync(activeId))!.Lifecycle;

            Assert.True(expired.PilotEndPassedNotAdvanced);
            Assert.Equal(BillingStatuses.Pilot, expired.BillingStatus);
            Assert.False(active.PilotEndPassedNotAdvanced);
            Assert.True(expired.StoredAtUtc > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task Profile_ScheduledCancel_ShowsCancelAtRenewal()
        {
            var renewal = DateTime.UtcNow.AddDays(14);
            var id = SeedWorkspace(
                "Cancel Venue",
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                renewal: renewal,
                scheduledCancel: true
            );
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            var scheduled = profile!.Lifecycle.ScheduledChange;
            Assert.True(scheduled.CancelScheduled);
            Assert.Equal(renewal, scheduled.CancelAtUtc);
            Assert.Equal(renewal, scheduled.EffectiveAtUtc);
            Assert.Equal(renewal, profile.Lifecycle.CurrentPeriodEndUtc);
        }

        [Fact]
        public async Task Profile_ScheduledTermSwitch_ShowsTargetAndEffectiveDate()
        {
            var renewal = DateTime.UtcNow.AddDays(30);
            var id = SeedWorkspace(
                "Switch Venue",
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                renewal: renewal,
                scheduledTarget: (BillingSubscriptionPlans.Starter, BillingCycles.Annual)
            );
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            var scheduled = profile!.Lifecycle.ScheduledChange;
            Assert.True(scheduled.HasScheduledChange);
            Assert.Equal(BillingSubscriptionPlans.Starter, scheduled.TargetSubscriptionPlan);
            Assert.Equal(BillingCycles.Annual, scheduled.TargetBillingCycle);
            Assert.Equal(renewal, scheduled.EffectiveAtUtc);
            Assert.Null(scheduled.CancelAtUtc);
        }

        [Fact]
        public async Task Profile_Locations_CountsIncludedPaidExtraActiveAndTotal()
        {
            var book = PricebookCatalog.LoadFromDirectory(TestPricebookPaths.PackDirectory());
            var expectedIncluded = book.GetRequired(Pricebook).Plans["growth"].IncludedLocations;
            var id = SeedWorkspace(
                "Counts Venue",
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                paidExtra: 2
            );
            AddLocation(id, "Main", LocationLifecycleStatus.Active);
            AddLocation(id, "Second", LocationLifecycleStatus.Active);
            AddLocation(id, "Paused", LocationLifecycleStatus.Paused);
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            var counts = profile!.Locations.Counts;
            Assert.Equal(expectedIncluded, counts.Included);
            Assert.Equal(2, counts.PaidExtra);
            Assert.Equal(2, counts.Active);
            Assert.Equal(3, counts.Total);
            Assert.Equal(3, profile.Locations.Items.Count);
            Assert.Contains(profile.Locations.Items, row => row.Name == "Paused" && row.LifecycleStatus == "Paused");
        }

        [Fact]
        public async Task Profile_Locations_AssignedManagerCountFollowsNamedScope()
        {
            var id = SeedWorkspace("Managers Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            var managed = AddLocation(id, "Managed", LocationLifecycleStatus.Active);
            AddLocation(id, "Unmanaged", LocationLifecycleStatus.Active);
            var manager = AddUser("Area Lead", "lead@example.com");
            _context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = manager,
                RestaurantId = id,
                PermissionRole = PermissionRoles.LocationManager,
                LocationScope = LocationScopeKind.NamedList,
                NamedLocationIdsJson = $"[{managed}]",
                Status = MembershipStatus.Active,
            });
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            Assert.Equal(1, profile!.Locations.Items.Single(row => row.LocationId == managed).AssignedManagerCount);
            Assert.Equal(0, profile.Locations.Items.Single(row => row.Name == "Unmanaged").AssignedManagerCount);
        }

        [Fact]
        public async Task Profile_Capacity_SeatsCountActiveMembersAndLiveInvitesOnly()
        {
            var id = SeedWorkspace("Seats Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            var member = AddUser("Staff Member", "staff@example.com");
            _context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member,
                RestaurantId = id,
                PermissionRole = PermissionRoles.Staff,
                Status = MembershipStatus.Active,
            });
            AddInvite(id, "live@example.com", expiresAt: DateTime.UtcNow.AddDays(3));
            AddInvite(id, "expired@example.com", expiresAt: DateTime.UtcNow.AddDays(-1));
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            // Owner membership is not seeded, so this is the one active member plus one live invite.
            Assert.Equal(2, profile!.Capacity.Seats.Current);
            Assert.True(profile.Capacity.Seats.Available);
            var invite = Assert.Single(profile.Capacity.PendingInvites);
            Assert.Equal("l••••@example.com", invite.MaskedEmail);
        }

        [Fact]
        public async Task Profile_Capacity_OffersAndQrPoolReportAvailability()
        {
            var id = SeedWorkspace("Pools Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            Assert.True(profile!.Capacity.QrPlacements.Available);
            Assert.True(profile.Capacity.Offers.Available);
            Assert.Equal(0, profile.Capacity.Offers.Current);
        }

        [Fact]
        public async Task Profile_Credits_ReturnsThreeChannelsInOrder()
        {
            var id = SeedWorkspace("Credits Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            Assert.Equal(CreditChannels.All, profile!.Credits.Select(row => row.Channel));
        }

        [Fact]
        public async Task Profile_Payments_NewestTwentyForWorkspace_WithRedactedReference()
        {
            var id = SeedWorkspace("Payments Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            var other = SeedWorkspace("Other Payments", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            for (var index = 0; index < 21; index++)
            {
                AddIntent(id, $"order-{index:D2}-abcdef", createdAt: DateTime.UtcNow.AddMinutes(-index));
            }
            AddIntent(other, "foreign-order-zzzzzz", createdAt: DateTime.UtcNow);
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            Assert.Equal(20, profile!.Payments.Count);
            Assert.Equal("••••abcdef", profile.Payments[0].ProviderReference);
            Assert.Equal(1500, profile.Payments[0].AmountPence);
            Assert.Equal("GBP", profile.Payments[0].Currency);
            Assert.DoesNotContain(profile.Payments, row => row.ProviderReference == "••••zzzzzz");
            Assert.Equal(
                profile.Payments.OrderByDescending(row => row.CreatedAtUtc).Select(row => row.Id),
                profile.Payments.Select(row => row.Id)
            );
        }

        [Fact]
        public async Task Profile_Invoices_ListInvoicesAndCreditNotes_ForWorkspaceOnly()
        {
            var id = SeedWorkspace("Invoices Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            var other = SeedWorkspace("Other Invoices", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            AddInvoice(id, "TM-000101", TummlyVatInvoice.PrefixTm, 1200, DateTime.UtcNow.AddDays(-2));
            AddInvoice(id, "TCN-000007", TummlyVatInvoice.PrefixTcn, 600, DateTime.UtcNow.AddDays(-1));
            AddInvoice(other, "TM-000999", TummlyVatInvoice.PrefixTm, 999, DateTime.UtcNow);
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            Assert.Equal(2, profile!.Invoices.Count);
            Assert.Equal("TCN-000007", profile.Invoices[0].DocumentNumber);
            Assert.Equal("Credit note", profile.Invoices[0].Type);
            Assert.Equal("Invoice", profile.Invoices[1].Type);
            Assert.Equal(1200, profile.Invoices[1].GrossPence);
            Assert.DoesNotContain(profile.Invoices, row => row.DocumentNumber == "TM-000999");
        }

        [Fact]
        public async Task Profile_RecentAudit_TwentyForWorkspaceOnly()
        {
            var id = SeedWorkspace("Audit Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            var other = SeedWorkspace("Other Audit", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            for (var index = 0; index < 21; index++)
            {
                AddAudit(id, $"own-{index}", DateTime.UtcNow.AddMinutes(-index));
            }
            AddAudit(other, "foreign-audit", DateTime.UtcNow);
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            Assert.Equal(20, profile!.RecentAudit.Count);
            Assert.All(profile.RecentAudit, row => Assert.Equal(id, row.RestaurantId));
            Assert.DoesNotContain(profile.RecentAudit, row => row.TargetId == "foreign-audit");
        }

        [Fact]
        public async Task Profile_Isolation_OtherWorkspaceDataNeverAppears()
        {
            var id = SeedWorkspace(
                "Isolated A",
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                ownerEmail: "owner-a@example.com"
            );
            var other = SeedWorkspace(
                "Isolated B",
                plan: BillingSubscriptionPlans.Growth,
                status: BillingStatuses.Active,
                ownerEmail: "owner-b@example.com"
            );
            AddLocation(other, "Secret Location B", LocationLifecycleStatus.Active);
            AddInvite(other, "invitee-b@example.com", expiresAt: DateTime.UtcNow.AddDays(3));
            AddInvoice(other, "TM-SECRET-B", TummlyVatInvoice.PrefixTm, 4321, DateTime.UtcNow);
            AddIntent(other, "secret-order-b-123456", createdAt: DateTime.UtcNow);
            AddAudit(other, "secret-audit-b", DateTime.UtcNow);
            AddInvoice(id, "TM-OWN-A", TummlyVatInvoice.PrefixTm, 100, DateTime.UtcNow);
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            var json = JsonSerializer.Serialize(profile);
            Assert.Contains("TM-OWN-A", json);
            Assert.DoesNotContain("owner-b@example.com", json);
            Assert.DoesNotContain("Secret Location B", json);
            Assert.DoesNotContain("TM-SECRET-B", json);
            Assert.DoesNotContain("secret-order-b-123456", json);
            Assert.DoesNotContain("secret-audit-b", json);
            Assert.DoesNotContain("invitee-b@example.com", json);
            Assert.DoesNotContain("Isolated B", json);
        }

        [Fact]
        public async Task Profile_InviteEmails_AreMaskedNotRaw()
        {
            var id = SeedWorkspace("Mask Venue", plan: BillingSubscriptionPlans.Growth, status: BillingStatuses.Active);
            AddInvite(id, "jane.doe@example.com", expiresAt: DateTime.UtcNow.AddDays(3));
            await _context.SaveChangesAsync();

            var profile = await _service.GetProfileAsync(id);

            var json = JsonSerializer.Serialize(profile);
            Assert.DoesNotContain("jane.doe@example.com", json);
            Assert.Equal("j••••@example.com", Assert.Single(profile!.Capacity.PendingInvites).MaskedEmail);
        }

        private int SeedWorkspace(
            string name,
            string plan = BillingSubscriptionPlans.Pilot,
            string status = BillingStatuses.Pilot,
            string ownerEmail = "owner@example.com",
            string? legal = null,
            DateTime? pilotStart = null,
            DateTime? pilotEnd = null,
            DateTime? renewal = null,
            DateTime? softLockAt = null,
            bool scheduledCancel = false,
            (string Plan, string Cycle)? scheduledTarget = null,
            int paidExtra = 0
        )
        {
            var created = DateTime.UtcNow;
            var ownerId = AddUser("Owner Person", ownerEmail);

            var billing = new BillingAccount
            {
                SubscriptionPlan = plan,
                BillingStatus = status,
                BillingCycle = plan == BillingSubscriptionPlans.Pilot ? null : BillingCycles.Monthly,
                ContractedPricebookId = Pricebook,
                PilotStartedAtUtc = pilotStart,
                PilotPeriodEnd = pilotEnd,
                RenewalDateUtc = renewal,
                SoftLockEnteredAt = softLockAt,
                PaidExtraLocationCount = paidExtra,
                ScheduledCancelPlan = scheduledCancel,
                HasScheduledChange = scheduledTarget != null || scheduledCancel,
                ScheduledTargetSubscriptionPlan = scheduledTarget?.Plan,
                ScheduledTargetBillingCycle = scheduledTarget?.Cycle,
            };
            var restaurant = new Restaurant
            {
                Name = name,
                AccountType = "Single",
                OwnerUserId = ownerId,
                BillingContactUserId = ownerId,
                PrivacyContactUserId = ownerId,
                SupportContactUserId = ownerId,
                CreatedAt = created,
                BillingAccount = billing,
            };
            if (legal != null)
            {
                restaurant.BusinessDetails = new RestaurantBusinessDetails { LegalBusinessName = legal };
            }
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            return restaurant.Id;
        }

        private int AddUser(string fullName, string email)
        {
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = "x",
                Role = "Owner",
                PhoneNumber = "07700900444",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return user.Id;
        }

        private int AddLocation(int restaurantId, string name, LocationLifecycleStatus status)
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = name,
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                LifecycleStatus = status,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();
            return location.Id;
        }

        private void AddInvite(int restaurantId, string email, DateTime expiresAt)
        {
            _context.TeamInvitations.Add(new TeamInvitation
            {
                RestaurantId = restaurantId,
                Email = email,
                FullName = "Invitee",
                PermissionRole = PermissionRoles.Staff,
                NamedLocationIdsJson = "[]",
                InviterUserId = 0,
                SentAt = DateTime.UtcNow.AddDays(-1),
                ExpiresAt = expiresAt,
                OpaqueReference = Guid.NewGuid().ToString("N"),
            });
            _context.SaveChanges();
        }

        private void AddIntent(int restaurantId, string orderId, DateTime createdAt)
        {
            _context.RevolutOrderIntents.Add(new RevolutOrderIntent
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                RestaurantId = restaurantId,
                Purpose = RevolutOrderIntentPurposes.Topup,
                GrossAmountMinor = 1500,
                IsOpen = false,
                CreatedAtUtc = createdAt,
            });
            _context.SaveChanges();
        }

        private void AddInvoice(int restaurantId, string number, string prefix, int gross, DateTime invoiceDate)
        {
            _context.TummlyVatInvoices.Add(new TummlyVatInvoice
            {
                Id = Guid.NewGuid(),
                DocumentNumber = number,
                DocumentPrefix = prefix,
                RevolutOrderId = Guid.NewGuid().ToString("N"),
                RestaurantId = restaurantId,
                InvoiceDateUtc = invoiceDate,
                TaxPointUtc = invoiceDate,
                LineDescription = "Plan",
                GrossPence = gross,
            });
            _context.SaveChanges();
        }

        private void AddAudit(int restaurantId, string targetId, DateTime occurredAt)
        {
            _context.AdminAuditEvents.Add(new AdminAuditEvent
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = occurredAt,
                Action = AdminAuditActions.CreditAdjust,
                ActorIdentity = "staff@tummly.com",
                TargetType = AdminAuditTargetTypes.Restaurant,
                TargetId = targetId,
                RestaurantId = restaurantId,
                Succeeded = true,
            });
            _context.SaveChanges();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public sealed class GuestResponseEmailDeliveryServiceTests : IDisposable
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly ServiceProvider _services;
        private readonly ApplicationDbContext _context;
        private readonly TrackingGuestResponseEmailService _emailService;
        private readonly CapturingLogger<GuestResponseEmailDeliveryWork> _deliveryLogger;
        private readonly IGuestResponseEmailDeliveryWork _deliveryWork;
        private readonly FeedbackGuestResponsesService _guestResponses;
        private readonly FeedbackRespondAndRecordService _respondAndRecord;
        private readonly FeedbackRecoveryOffersService _recoveryOffers;

        public GuestResponseEmailDeliveryServiceTests()
        {
            _emailService = new TrackingGuestResponseEmailService();
            _deliveryLogger = new CapturingLogger<GuestResponseEmailDeliveryWork>();

            var collection = new ServiceCollection();
            collection.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w =>
                        w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                    )
            );
            collection.AddSingleton<IEmailService>(_emailService);
            collection.AddSingleton<IConfiguration>(
                new ConfigurationBuilder().Build()
            );
            collection.AddSingleton<IOptions<GuestResponseEmailDeliverySettings>>(
                Options.Create(
                    new GuestResponseEmailDeliverySettings
                    {
                        RetryBackoffSeconds = 0,
                        ClaimLeaseMinutes = 10,
                        SweepIntervalSeconds = 30,
                    }
                )
            );
            collection.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
            collection.AddSingleton<ILogger<GuestResponseEmailDeliveryWork>>(
                _deliveryLogger
            );
            collection.AddSingleton<
                IGuestResponseEmailDeliveryWork,
                GuestResponseEmailDeliveryWork
            >();

            _services = collection.BuildServiceProvider();
            _deliveryWork =
                _services.GetRequiredService<IGuestResponseEmailDeliveryWork>();
            _context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w =>
                        w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                    )
                    .Options
            );
            _guestResponses = new FeedbackGuestResponsesService(
                _context,
                _deliveryWork,
                new LiveRecoverySmsBillingReserve(
                    _context,
                    new CreditLedgerService(
                        _context,
                        TimeProvider.System,
                        new DeliveryTestPricebookCatalog(),
                        new AdminAuditService(_context, TimeProvider.System)
                    ),
                    new CreditBalanceSnapshotService(_context, TimeProvider.System)
                ),
                new NoOpRecoveryGuestSmsDelivery(),
                TimeProvider.System,
                new GatedGuestReplyLock()
            );
            _respondAndRecord = new FeedbackRespondAndRecordService(
                _context,
                _deliveryWork
            );
            _recoveryOffers = new FeedbackRecoveryOffersService(
                _context,
                _deliveryWork,
                new OfferIssueService(_context),
                new GatedGuestReplyLock()
            );
        }

        public void Dispose()
        {
            _context.Dispose();
            _services.Dispose();
        }

        [Fact]
        public async Task SendAsync_Email_PersistsFactThenDrainDeliversNoOfferMail()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "guest@example.com"
            );

            var result = await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks for visiting",
                body: "Hi guest, thanks for your feedback.",
                purpose: "thank_the_guest",
                tone: "warm_and_friendly",
                includeNotes: null
            );

            Assert.NotNull(result);
            Assert.Equal(0, _emailService.CallCount);

            var pending = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Pending,
                pending.EmailDeliveryStatus
            );

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("guest@example.com", _emailService.LastToEmail);
            Assert.Equal("Thanks for visiting", _emailService.LastSubject);
            Assert.Equal("Recovery Venue", _emailService.LastBrandTitle);
            Assert.Equal("Main", _emailService.LastBrandSubtitle);
            Assert.Equal("1 High Street", _emailService.LastLocationAddress);
            Assert.Equal(
                "Hi guest, thanks for your feedback.",
                _emailService.LastMessage
            );
            Assert.Null(_emailService.LastOffer);
            Assert.False(string.IsNullOrWhiteSpace(_emailService.LastUnsubscribeHref));
            Assert.Contains(
                "/unsubscribe",
                _emailService.LastUnsubscribeHref!,
                StringComparison.Ordinal
            );

            var accepted = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.Id == pending.Id);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                accepted.EmailDeliveryStatus
            );
            Assert.NotNull(accepted.EmailDeliveredAt);
            Assert.Null(accepted.EmailDeliveryClaimedAt);
            Assert.Null(accepted.EmailDeliveryRetryAfter);
        }

        [Fact]
        public async Task SendAsync_Email_ConfirmSucceedsWhileDeliveryPending_RetriesUntilAccepted()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "retry-guest@example.com"
            );
            _emailService.ThrowOnSend = true;

            var result = await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Subject",
                body: "Body text for retry",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            Assert.NotNull(result);
            Assert.Equal(1, await _context.FeedbackGuestResponses.CountAsync());

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            var afterFail = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Pending,
                afterFail.EmailDeliveryStatus
            );
            Assert.Null(afterFail.EmailDeliveryClaimedAt);
            Assert.True(afterFail.EmailDeliveryAttemptCount >= 1);
            Assert.NotNull(afterFail.EmailDeliveryRetryAfter);

            // Make the row claimable again without waiting on backoff.
            // Clear the send-path tracker so we do not overwrite delivery updates.
            _context.ChangeTracker.Clear();
            var tracked = await _context.FeedbackGuestResponses.SingleAsync();
            tracked.EmailDeliveryRetryAfter = null;
            await _context.SaveChangesAsync();

            _emailService.ThrowOnSend = false;
            await _deliveryWork.DrainAsync();

            Assert.Equal(2, _emailService.CallCount);
            Assert.Equal("retry-guest@example.com", _emailService.LastToEmail);

            var accepted = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                accepted.EmailDeliveryStatus
            );
        }

        [Fact]
        public async Task SendAsync_Sms_SavesFactOnly_NoEmailDelivery()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Phone,
                guestContact: "07911123456"
            );

            var result = await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Sms,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: null,
                body: "Thanks for your visit.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            Assert.NotNull(result);

            var row = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.NotApplicable,
                row.EmailDeliveryStatus
            );
            Assert.False(string.IsNullOrWhiteSpace(row.BillingReservationRef));

            await _deliveryWork.DrainAsync();

            Assert.Equal(0, _emailService.CallCount);
        }

        [Fact]
        public async Task SendAndRecordAsync_Email_PersistsThenDrainDelivers()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "record-guest@example.com"
            );

            var result = await _respondAndRecord.SendAndRecordAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackInternalActionCategory.ProductQualityChecked,
                note: "Spoke with kitchen lead",
                subject: "We are on it",
                body: "Thanks — we are fixing this.",
                purpose: "apologise_and_explain",
                tone: "warm_and_apologetic",
                includeNotes: null
            );

            Assert.NotNull(result);
            Assert.Equal(0, _emailService.CallCount);

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("record-guest@example.com", _emailService.LastToEmail);
            Assert.Equal("We are on it", _emailService.LastSubject);
            Assert.Null(_emailService.LastOffer);

            var accepted = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                accepted.EmailDeliveryStatus
            );
            Assert.Equal(1, await _context.FeedbackInternalActions.CountAsync());
        }

        [Fact]
        public async Task Delivery_SendsWhenMarketingNotGranted_FollowUpGranted()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "follow-up-only@example.com",
                grantEmailMarketing: false
            );

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks",
                body: "Follow-up without marketing.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            Assert.Equal(0, _emailService.CallCount);

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("follow-up-only@example.com", _emailService.LastToEmail);
        }

        [Fact]
        public async Task Delivery_SkipsWhenFollowUpWithdrawnAfterCompose()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "withdrawn-after@example.com"
            );

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks",
                body: "Will be skipped after withdraw.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            var pending = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Pending,
                pending.EmailDeliveryStatus
            );

            await WithdrawFollowUpAsync(seeded.FeedbackId);

            await _deliveryWork.DrainAsync();

            Assert.Equal(0, _emailService.CallCount);

            var skipped = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.Id == pending.Id);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Abandoned,
                skipped.EmailDeliveryStatus
            );
            Assert.Equal(GuestReplyDeliveryState.Failed, skipped.DeliveryState);
            Assert.Null(skipped.EmailDeliveredAt);
            Assert.Null(skipped.EmailDeliveryClaimedAt);
            Assert.Null(skipped.EmailDeliveryRetryAfter);
        }

        [Fact]
        public async Task Delivery_ProviderFailure_SchedulesRetry()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "provider-fail@example.com"
            );
            _emailService.ThrowOnSend = true;

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Subject",
                body: "Body",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            var afterFail = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Pending,
                afterFail.EmailDeliveryStatus
            );
            Assert.Null(afterFail.EmailDeliveryClaimedAt);
            Assert.NotNull(afterFail.EmailDeliveryRetryAfter);
        }

        [Fact]
        public async Task Delivery_ProviderFailure_LogOmitsExceptionObjectAndRawEmail()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "provider-fail@example.com"
            );
            _emailService.ThrowOnSend = true;
            _emailService.ThrowMessage =
                "Resend rejected recipient provider-fail@example.com";
            _deliveryLogger.Entries.Clear();

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Subject",
                body: "Body",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            await _deliveryWork.DrainAsync();

            var failureLogs = _deliveryLogger.Entries
                .Where(e =>
                    e.Level == LogLevel.Error
                    && e.Message.Contains(
                        "Resend failed",
                        StringComparison.Ordinal
                    )
                )
                .ToList();
            Assert.NotEmpty(failureLogs);
            foreach (var entry in failureLogs)
            {
                Assert.Null(entry.Exception);
                Assert.DoesNotContain(
                    "provider-fail@example.com",
                    entry.Message
                );
                Assert.Contains("InvalidOperationException", entry.Message);
            }
        }

        [Fact]
        public async Task Delivery_RunTwice_SendsOnce()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "once-only@example.com"
            );

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks",
                body: "Deliver once.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            await _deliveryWork.DrainAsync();
            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
        }

        [Fact]
        public async Task SendAndIssueAsync_Email_PersistsThenDrainDeliversOfferWithIssuedCode()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "offer-guest@example.com",
                withRecoveryCatalogAttach: true
            );

            var result = await _recoveryOffers.SendAndIssueAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                new SendAndIssueFeedbackRecoveryOfferRequest
                {
                    Channel = "email",
                    Subject = "A recovery offer from us",
                    Body = "Please enjoy 20% off your next visit.",
                    Intent = "respond_with_recovery_offer",
                    Purpose = "include_a_recovery_offer",
                    Tone = "warm_and_apologetic",
                }
            );

            Assert.NotNull(result);
            Assert.StartsWith("TUM-", result!.RecoveryOffer.RedemptionCode);
            Assert.Equal(0, _emailService.CallCount);

            var pending = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Pending,
                pending.EmailDeliveryStatus
            );

            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);
            Assert.Equal("offer-guest@example.com", _emailService.LastToEmail);
            Assert.Equal("A recovery offer from us", _emailService.LastSubject);
            Assert.NotNull(_emailService.LastOffer);
            Assert.Equal("20% off next visit", _emailService.LastOffer!.Title);
            Assert.Equal(
                "Thanks for your feedback — enjoy 20% off.",
                _emailService.LastOffer.Description
            );
            Assert.Equal(
                result.RecoveryOffer.RedemptionCode,
                _emailService.LastOffer.RedemptionCode
            );
            Assert.StartsWith("Expires: ", _emailService.LastOffer.ExpiryLabel);

            var accepted = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.Id == pending.Id);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                accepted.EmailDeliveryStatus
            );
            Assert.Equal(0, await _context.FeedbackRecoveryOffers.CountAsync());
            Assert.Equal(1, await _context.OfferIssues.CountAsync());
        }

        [Fact]
        public async Task Worker_Accepted_SetsProviderIdAndAccepted()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "accepted-id@example.com"
            );
            _emailService.ProviderMessageIdToReturn = "re_123";

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks",
                body: "Accepted by Resend.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            await _deliveryWork.DrainAsync();

            var row = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal("re_123", row.ProviderMessageId);
            Assert.Equal(GuestReplyDeliveryState.Accepted, row.DeliveryState);
            Assert.NotNull(row.DeliveryStateAtUtc);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Accepted,
                row.EmailDeliveryStatus
            );
        }

        [Fact]
        public async Task Worker_NotEligible_SetsFailed()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "not-eligible@example.com"
            );

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks",
                body: "Will not be eligible.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            await WithdrawFollowUpAsync(seeded.FeedbackId);

            await _deliveryWork.DrainAsync();

            Assert.Equal(0, _emailService.CallCount);

            var row = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(GuestReplyDeliveryState.Failed, row.DeliveryState);
            Assert.NotNull(row.DeliveryStateAtUtc);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Abandoned,
                row.EmailDeliveryStatus
            );

            await _deliveryWork.DrainAsync();

            Assert.Equal(0, _emailService.CallCount);
        }

        [Fact]
        public async Task Worker_FiveFailures_SetsFailedAndStops()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "five-failures@example.com"
            );
            _emailService.ThrowOnSend = true;

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Subject",
                body: "Body that keeps failing.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            for (var attempt = 0; attempt < 5; attempt++)
            {
                await _deliveryWork.DrainAsync();

                // Make the row claimable again without waiting on backoff.
                _context.ChangeTracker.Clear();
                var tracked = await _context.FeedbackGuestResponses.SingleAsync();
                tracked.EmailDeliveryRetryAfter = null;
                await _context.SaveChangesAsync();
            }

            Assert.Equal(5, _emailService.CallCount);

            var row = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(GuestReplyDeliveryState.Failed, row.DeliveryState);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Abandoned,
                row.EmailDeliveryStatus
            );
            Assert.Null(row.EmailDeliveryClaimedAt);

            _emailService.ThrowOnSend = false;
            await _deliveryWork.DrainAsync();

            Assert.Equal(5, _emailService.CallCount);
        }

        [Fact]
        public async Task Worker_DemoSuppressed_SetsUnavailableWithoutProviderId()
        {
            var seeded = await SeedAsync(
                contactType: ContactType.Email,
                guestContact: "demo-suppressed@example.com"
            );
            _emailService.SuppressSend = true;

            await _guestResponses.SendAsync(
                seeded.FeedbackId,
                seeded.AuthorUserId,
                FeedbackGuestResponseChannel.Email,
                FeedbackRecoveryIntent.RespondToGuest,
                subject: "Thanks",
                body: "Suppressed by demo mode.",
                purpose: null,
                tone: null,
                includeNotes: null
            );

            await _deliveryWork.DrainAsync();
            await _deliveryWork.DrainAsync();

            Assert.Equal(1, _emailService.CallCount);

            var row = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(GuestReplyDeliveryState.Unavailable, row.DeliveryState);
            Assert.NotNull(row.DeliveryStateAtUtc);
            Assert.Null(row.ProviderMessageId);
            Assert.Equal(
                GuestResponseEmailDeliveryStatus.Abandoned,
                row.EmailDeliveryStatus
            );
            Assert.Null(row.EmailDeliveredAt);
            Assert.Null(row.EmailDeliveryClaimedAt);
        }

        private async Task WithdrawFollowUpAsync(int feedbackId)
        {
            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == feedbackId);
            Assert.NotNull(feedback.LocationGuestId);

            var permissions = new LocationGuestPermissionLedgerService(_context);
            permissions.RecordEvent(
                feedback.LocationGuestId!.Value,
                feedback.RestaurantLocationId,
                LocationGuestPermissionKind.FeedbackFollowUp,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.GuestForm,
                new DateTime(2026, 10, 24, 14, 0, 0, DateTimeKind.Utc)
            );
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        private async Task<(int FeedbackId, int AuthorUserId)> SeedAsync(
            ContactType contactType,
            string guestContact,
            bool withRecoveryCatalogAttach = false,
            bool grantEmailMarketing = true
        )
        {
            var user = new User
            {
                FullName = "Delivery Operator",
                Email = $"op-{Guid.NewGuid()}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900999",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Recovery Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            _context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            int? locationGuestId = null;
            int? recoveryOfferId = null;

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = contactType == ContactType.Email
                    ? guestContact.Trim().ToLowerInvariant()
                    : null,
                NormalizedEmail = contactType == ContactType.Email
                    ? guestContact.Trim().ToLowerInvariant()
                    : null,
                Mobile = contactType == ContactType.Phone ? guestContact : null,
                NormalizedPhone = contactType == ContactType.Phone
                    ? new string(guestContact.Where(char.IsDigit).ToArray())
                    : null,
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = master.Id,
                Name = "Alex Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(locationGuest);
            await _context.SaveChangesAsync();
            locationGuestId = locationGuest.Id;

            var permissions = new LocationGuestPermissionLedgerService(_context);
            var grantAt = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            permissions.RecordEvent(
                locationGuest.Id,
                locationGuest.RestaurantLocationId,
                LocationGuestPermissionKind.FeedbackFollowUp,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                grantAt
            );
            if (contactType == ContactType.Email && grantEmailMarketing)
            {
                permissions.RecordEvent(
                    locationGuest.Id,
                    locationGuest.RestaurantLocationId,
                    LocationGuestPermissionKind.EmailMarketing,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    grantAt
                );
            }
            else if (contactType == ContactType.Phone)
            {
                permissions.RecordEvent(
                    locationGuest.Id,
                    locationGuest.RestaurantLocationId,
                    LocationGuestPermissionKind.SmsMarketing,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    grantAt
                );
            }
            await _context.SaveChangesAsync();

            if (withRecoveryCatalogAttach)
            {
                var catalogOffer = new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Status = "active",
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "20% off next visit",
                    Description = "Thanks for your feedback — enjoy 20% off.",
                    Validity = CatalogOfferValidity.Days30AfterIssue,
                    DiscountPercentage = 20m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                _context.CatalogOffers.Add(catalogOffer);
                await _context.SaveChangesAsync();
                recoveryOfferId = catalogOffer.Id;
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuestId,
                RecoveryOfferId = recoveryOfferId,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[\"FoodQuality\"]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            if (contactType == ContactType.Phone)
            {
                _context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = restaurant.Id,
                        Channel = CreditChannels.Sms,
                        EntryType = CreditLedgerEntryTypes.PilotAllocation,
                        Quantity = 20,
                        PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await _context.SaveChangesAsync();
            }

            return (feedback.Id, user.Id);
        }

        private sealed class TrackingGuestResponseEmailService : EmailServiceStubBase
        {
            public int CallCount { get; private set; }

            public string? LastToEmail { get; private set; }

            public string? LastSubject { get; private set; }

            public string? LastBrandTitle { get; private set; }

            public string? LastBrandSubtitle { get; private set; }

            public string? LastLocationAddress { get; private set; }

            public string? LastMessage { get; private set; }

            public GuestResponseEmailOfferBlock? LastOffer { get; private set; }

            public string? LastUnsubscribeHref { get; private set; }

            public bool ThrowOnSend { get; set; }

            public string ThrowMessage { get; set; } = "Resend failed";

            public string? ProviderMessageIdToReturn { get; set; } = "re_123";

            public bool SuppressSend { get; set; }

            public override Task<GuestResponseEmailSendResult> SendGuestResponseEmailAsync(
                string toEmail,
                string subject,
                string brandTitle,
                string? brandSubtitle,
                string? locationAddress,
                string message,
                string? brandLogoUrl = null,
                GuestResponseEmailOfferBlock? offer = null,
                string? unsubscribeHref = null,
                string? ticketSubject = null
            )
            {
                CallCount++;
                LastToEmail = toEmail;
                LastSubject = subject;
                LastBrandTitle = brandTitle;
                LastBrandSubtitle = brandSubtitle;
                LastLocationAddress = locationAddress;
                LastMessage = message;
                LastOffer = offer;
                LastUnsubscribeHref = unsubscribeHref;

                if (ThrowOnSend)
                {
                    throw new InvalidOperationException(ThrowMessage);
                }

                return Task.FromResult(
                    SuppressSend
                        ? new GuestResponseEmailSendResult(null, Suppressed: true)
                        : new GuestResponseEmailSendResult(ProviderMessageIdToReturn, Suppressed: false)
                );
            }
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<(
                LogLevel Level,
                Exception? Exception,
                string Message
            )> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            )
            {
                Entries.Add((logLevel, exception, formatter(state, exception)));
            }
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Testing";

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } = ".";

            public IFileProvider ContentRootFileProvider { get; set; } =
                new NullFileProvider();
        }

        private sealed class NoOpRecoveryGuestSmsDelivery : IRecoveryGuestSmsDelivery
        {
            public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
                string phoneNumber,
                string body,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<RecoveryGuestSmsDeliveryResult>(
                    new RecoveryGuestSmsDeliveryResult.Accepted
                    {
                        AcceptedSegments = 1,
                    }
                );
        }

        private sealed class DeliveryTestPricebookCatalog : IPricebookCatalog
        {
            public string CurrentPricebookId => "TUMMLY-UK-GBP-2026-08-V3";

            public PricebookSnapshot GetRequired(string pricebookId) =>
                throw new NotImplementedException();

            public string FormatPlanPriceNet(PricebookPlan plan, string? billingCycle) =>
                throw new NotImplementedException();

            public string FormatIncludedCreditsLabel(PricebookPlan plan, string channel) =>
                throw new NotImplementedException();

            public BillingCurrentCatalogDto BuildCurrentCatalog(bool sms5000Available) =>
                throw new NotImplementedException();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class ServiceReplyEligibilityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GuestContactSuppressionService _suppressions;
        private readonly DateTime _now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

        public ServiceReplyEligibilityTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _suppressions = new GuestContactSuppressionService(
                _context,
                new GuestContactHasher(GuestContactHasher.TestingFallbackKey)
            );
        }

        public void Dispose()
            => _context.Dispose();

        [Theory]
        [InlineData(FeedbackGuestResponseChannel.Email, "alex@example.com")]
        [InlineData(FeedbackGuestResponseChannel.Sms, "+447911123456")]
        public async Task Allowed_WhenFollowUpGranted_MarketingNotRecorded(
            FeedbackGuestResponseChannel channel,
            string guestContact
        )
        {
            var feedback = await SeedAsync(
                guestContact,
                followUpGranted: true,
                marketing: null
            );

            Assert.True(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    channel,
                    CancellationToken.None
                )
            );
        }

        [Theory]
        [InlineData(FeedbackGuestResponseChannel.Email, "alex@example.com")]
        [InlineData(FeedbackGuestResponseChannel.Sms, "+447911123456")]
        public async Task Allowed_WhenMarketingWithdrawn_FollowUpGranted(
            FeedbackGuestResponseChannel channel,
            string guestContact
        )
        {
            var feedback = await SeedAsync(
                guestContact,
                followUpGranted: true,
                marketing: LocationGuestPermissionState.Withdrawn
            );

            Assert.True(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    channel,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task Denied_WhenFollowUpWithdrawn()
        {
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: false,
                marketing: null
            );

            Assert.False(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task Denied_WhenRestaurantFollowUpDisabled()
        {
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: true,
                marketing: null,
                feedbackFollowUpEnabled: false
            );

            Assert.False(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task Denied_WhenNoLocationGuest()
        {
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: true,
                marketing: null,
                linkLocationGuest: false
            );

            Assert.False(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task Denied_WhenChannelDoesNotMatchContact()
        {
            var feedback = await SeedAsync(
                "+447911123456",
                followUpGranted: true,
                marketing: null
            );

            Assert.False(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Theory]
        [InlineData(FeedbackGuestResponseChannel.Sms, "+447911123456", LocationGuestPermissionKind.SmsMarketing, "sms-stop")]
        [InlineData(FeedbackGuestResponseChannel.Email, "alex@example.com", LocationGuestPermissionKind.FeedbackFollowUp, "guest-delete")]
        [InlineData(FeedbackGuestResponseChannel.Sms, "+447911123456", LocationGuestPermissionKind.EmailMarketing, "guest-delete")]
        [InlineData(FeedbackGuestResponseChannel.Sms, "+447911123456", LocationGuestPermissionKind.SmsMarketing, "retention-purge")]
        [InlineData(FeedbackGuestResponseChannel.Email, "alex@example.com", LocationGuestPermissionKind.EmailDelivery, "email-bounce")]
        public async Task Denied_WhenReplyChannelSuppressed(
            FeedbackGuestResponseChannel channel,
            string guestContact,
            LocationGuestPermissionKind kind,
            string source
        )
        {
            var feedback = await SeedAsync(
                guestContact,
                followUpGranted: true,
                marketing: null
            );
            await StageSuppressionAsync(feedback, guestContact, kind, source);

            Assert.False(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    channel,
                    CancellationToken.None
                )
            );
        }

        [Theory]
        [InlineData("email-unsubscribe")]
        [InlineData("email-complaint")]
        public async Task Allowed_WhenEmailMarketingUnsubscribedOrComplained(string source)
        {
            // Marketing permission and Feedback service email are separate purposes.
            // An EmailMarketing unsubscribe or complaint must not stop a genuine reply.
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: true,
                marketing: LocationGuestPermissionState.Withdrawn
            );
            await StageSuppressionAsync(
                feedback,
                "alex@example.com",
                LocationGuestPermissionKind.EmailMarketing,
                source
            );

            Assert.Equal(
                ServiceReplyDecision.Allowed,
                await ServiceReplyEligibility.EvaluateAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task Allowed_WhenEmailMarketingWithdrawnInLedger_ServiceEmailStillEligible()
        {
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: true,
                marketing: LocationGuestPermissionState.Withdrawn
            );

            Assert.Equal(
                ServiceReplyDecision.Allowed,
                await ServiceReplyEligibility.EvaluateAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task EmailHardBounce_RefusesWithSafeDeliveryMessage()
        {
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: true,
                marketing: null
            );
            await StageSuppressionAsync(
                feedback,
                "alex@example.com",
                LocationGuestPermissionKind.EmailDelivery,
                GuestContactSuppressionSources.EmailBounce
            );

            var decision = await ServiceReplyEligibility.EvaluateAsync(
                _context,
                _suppressions,
                feedback,
                FeedbackGuestResponseChannel.Email,
                CancellationToken.None
            );

            Assert.Equal(ServiceReplyDecision.EmailDeliveryFailure, decision);
            Assert.Equal(
                "Email delivery unavailable. Use a verified alternative contact method.",
                ServiceReplyEligibility.RefusalMessage(decision)
            );
            Assert.DoesNotContain(
                "alex",
                ServiceReplyEligibility.RefusalMessage(decision)!
            );
        }

        [Fact]
        public async Task SmsStop_ReturnsProviderRestriction_NotDeliveryMessage()
        {
            var feedback = await SeedAsync(
                "+447911123456",
                followUpGranted: true,
                marketing: null
            );
            await StageSuppressionAsync(
                feedback,
                "+447911123456",
                LocationGuestPermissionKind.SmsMarketing,
                GuestContactSuppressionSources.SmsStop
            );

            var decision = await ServiceReplyEligibility.EvaluateAsync(
                _context,
                _suppressions,
                feedback,
                FeedbackGuestResponseChannel.Sms,
                CancellationToken.None
            );

            Assert.Equal(ServiceReplyDecision.SmsProviderRestriction, decision);
            Assert.Equal(
                ServiceReplyEligibility.NotAllowedMessage,
                ServiceReplyEligibility.RefusalMessage(decision)
            );
        }

        [Fact]
        public void ClassifySuppressions_EmailDeliveryDoesNotBlockSms()
        {
            var rows = new[]
            {
                NewRow(LocationGuestPermissionKind.EmailDelivery, GuestContactSuppressionSources.EmailBounce),
            };

            Assert.Equal(
                ServiceReplyDecision.Allowed,
                ServiceReplyEligibility.ClassifySuppressions(rows, FeedbackGuestResponseChannel.Sms)
            );
        }

        [Fact]
        public void ClassifySuppressions_FeedbackFollowUpRowBlocksEveryChannel()
        {
            var rows = new[]
            {
                NewRow(LocationGuestPermissionKind.FeedbackFollowUp, GuestContactSuppressionSources.GuestDelete),
            };

            Assert.Equal(
                ServiceReplyDecision.NotEligible,
                ServiceReplyEligibility.ClassifySuppressions(rows, FeedbackGuestResponseChannel.Email)
            );
            Assert.Equal(
                ServiceReplyDecision.NotEligible,
                ServiceReplyEligibility.ClassifySuppressions(rows, FeedbackGuestResponseChannel.Sms)
            );
        }

        private static GuestContactSuppression NewRow(LocationGuestPermissionKind kind, string source) =>
            new()
            {
                PermissionKind = kind,
                Source = source,
                ContactHash = "hash",
            };

        [Fact]
        public async Task Allowed_WhenOtherChannelUnsubscribed()
        {
            // Email unsubscribe does not stop an SMS reply, and SMS STOP does not stop email.
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: true,
                marketing: null
            );
            await StageSuppressionAsync(
                feedback,
                "alex@example.com",
                LocationGuestPermissionKind.SmsMarketing,
                "sms-stop"
            );

            Assert.True(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task Allowed_WhenSuppressionBelongsToAnotherRestaurant()
        {
            var feedback = await SeedAsync(
                "alex@example.com",
                followUpGranted: true,
                marketing: null
            );
            await _suppressions.StageAsync(
                feedback.RestaurantLocationId + 900000,
                "email:alex@example.com",
                LocationGuestPermissionKind.EmailMarketing,
                _now,
                GuestContactSuppressionSources.EmailUnsubscribe
            );
            await _context.SaveChangesAsync();

            Assert.True(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task SmsSendStep_ReserveRefuses_WhenSuppressedAfterCompose()
        {
            var feedback = await SeedAsync(
                "+447911123456",
                followUpGranted: true,
                marketing: null
            );
            Assert.True(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    _suppressions,
                    feedback,
                    FeedbackGuestResponseChannel.Sms,
                    CancellationToken.None
                )
            );

            await StageSuppressionAsync(
                feedback,
                "+447911123456",
                LocationGuestPermissionKind.SmsMarketing,
                "sms-stop"
            );

            var billing = new CountingSmsReserve();
            var step = new GuestReplySmsSendStep(
                _context,
                _suppressions,
                billing,
                new RecordingRecoveryGuestSmsDelivery(),
                TimeProvider.System,
                NullLogger<GuestReplySmsSendStep>.Instance
            );

            var refused = await Assert.ThrowsAsync<ArgumentException>(() =>
                step.ReserveAsync(
                    feedback,
                    "send-time-key",
                    "Thanks for your message.",
                    CancellationToken.None
                )
            );
            Assert.Equal(ServiceReplyEligibility.NotAllowedMessage, refused.Message);
            Assert.Equal(0, billing.ReserveCalls);
        }

        private sealed class CountingSmsReserve : IRecoverySmsBillingReserve
        {
            public int ReserveCalls { get; private set; }

            public bool IsLive => true;

            public Task<RecoverySmsBillingReserveResult> ReserveAsync(
                RecoverySmsBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
                ReserveCalls++;
                return Task.FromResult<RecoverySmsBillingReserveResult>(
                    new RecoverySmsBillingReserveResult.Ok
                    {
                        ReservationRef = "res-" + Guid.NewGuid().ToString("N"),
                    }
                );
            }

            public Task<RecoverySmsBillingSettleResult> SettleAsync(
                RecoverySmsBillingSettleRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<RecoverySmsBillingSettleResult>(
                new RecoverySmsBillingSettleResult.Ok()
            );

            public Task<RecoverySmsBillingReleaseResult> ReleaseAsync(
                RecoverySmsBillingReleaseRequest request,
                CancellationToken cancellationToken = default
            ) => Task.FromResult<RecoverySmsBillingReleaseResult>(
                new RecoverySmsBillingReleaseResult.Ok()
            );
        }

        private async Task StageSuppressionAsync(
            Feedback feedback,
            string guestContact,
            LocationGuestPermissionKind kind,
            string source
        )
        {
            Assert.True(
                GuestFormContactValidate.TryResolve(guestContact, out var contactType)
            );
            Assert.True(
                GuestContactKey.TryCreate(guestContact, contactType, out var contactKey)
            );
            var restaurantId = await _context.RestaurantLocations
                .Where(row => row.Id == feedback.RestaurantLocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();

            await _suppressions.StageAsync(
                restaurantId,
                contactKey,
                kind,
                _now,
                source
            );
            await _context.SaveChangesAsync();
        }

        private async Task<Feedback> SeedAsync(
            string guestContact,
            bool followUpGranted,
            LocationGuestPermissionState? marketing,
            bool feedbackFollowUpEnabled = true,
            bool linkLocationGuest = true
        )
        {
            Assert.True(
                GuestFormContactValidate.TryResolve(guestContact, out var contactType)
            );

            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = _now,
                ActivatedAt = _now,
                ActivationExpiresAt = _now.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Service Reply Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
                FeedbackFollowUpPermissionEnabled = feedbackFollowUpEnabled,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            int? locationGuestId = null;
            if (linkLocationGuest)
            {
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
                    CreatedAt = _now,
                };
                _context.MasterGuests.Add(master);
                await _context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = location.Id,
                    Name = "Alex Guest",
                    MarketingPreference = LocationGuestMarketingPreference.NotRecorded,
                    CreatedAt = _now,
                };
                _context.LocationGuests.Add(locationGuest);
                await _context.SaveChangesAsync();
                locationGuestId = locationGuest.Id;

                var ledger = new LocationGuestPermissionLedgerService(_context);
                if (followUpGranted)
                {
                    ledger.RecordEvent(
                        locationGuest.Id,
                        location.Id,
                        LocationGuestPermissionKind.FeedbackFollowUp,
                        LocationGuestPermissionLedgerEventKinds.Grant,
                        LocationGuestPermissionLedgerSources.GuestForm,
                        _now
                    );
                }
                else
                {
                    ledger.RecordEvent(
                        locationGuest.Id,
                        location.Id,
                        LocationGuestPermissionKind.FeedbackFollowUp,
                        LocationGuestPermissionLedgerEventKinds.Withdraw,
                        LocationGuestPermissionLedgerSources.GuestForm,
                        _now
                    );
                }

                if (marketing is { } marketingState)
                {
                    var marketingKind = contactType == ContactType.Email
                        ? LocationGuestPermissionKind.EmailMarketing
                        : LocationGuestPermissionKind.SmsMarketing;
                    var eventKind = marketingState == LocationGuestPermissionState.Granted
                        ? LocationGuestPermissionLedgerEventKinds.Grant
                        : LocationGuestPermissionLedgerEventKinds.Withdraw;
                    ledger.RecordEvent(
                        locationGuest.Id,
                        location.Id,
                        marketingKind,
                        eventKind,
                        LocationGuestPermissionLedgerSources.GuestForm,
                        _now
                    );
                }

                await _context.SaveChangesAsync();
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuestId,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Great food",
                CreatedAt = _now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return feedback;
        }
    }
}

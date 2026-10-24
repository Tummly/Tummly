using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    public class ServiceReplyEligibilityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly DateTime _now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

        public ServiceReplyEligibilityTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
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
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
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

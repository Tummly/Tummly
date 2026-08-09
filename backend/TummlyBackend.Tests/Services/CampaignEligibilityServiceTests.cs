using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ICampaignEligibilityService"/> — stage-1 Matched /
    /// Currently eligible / Excluded (+ channel counts) and missing-store honesty.
    /// </summary>
    public class CampaignEligibilityServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CampaignEligibilityService _eligibility;

        public CampaignEligibilityServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _eligibility = new CampaignEligibilityService(_context);
        }

        [Fact]
        public async Task EvaluateAsync_AllEligibleGuests_SplitsMatchedEligibleExcludedAndChannels()
        {
            var seeded = await SeedLocationAsync();
            await AddGuestAsync(
                seeded,
                "Eligible Both",
                email: "both@example.com",
                mobile: "07700900111",
                offersOptOut: false
            );
            await AddGuestAsync(
                seeded,
                "Eligible Email",
                email: "email@example.com",
                mobile: null,
                offersOptOut: false
            );
            await AddGuestAsync(
                seeded,
                "Opted Out",
                email: "out@example.com",
                mobile: "07700900222",
                offersOptOut: true
            );
            await AddGuestAsync(
                seeded,
                "No Contact",
                email: null,
                mobile: null,
                offersOptOut: false
            );

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "all-eligible-guests"
            );

            Assert.True(result.Evaluable);
            Assert.Equal(4, result.Matched);
            Assert.Equal(2, result.CurrentlyEligible);
            Assert.Equal(2, result.Excluded);
            Assert.Equal(2, result.EmailEligible);
            Assert.Equal(1, result.SmsEligible);
            Assert.Equal(
                CampaignEligibilityService.CheckSetVersion,
                result.CheckSetVersion
            );

            Assert.Equal(2, result.ExcludedReasons.Count);
            Assert.Contains(
                result.ExcludedReasons,
                r => r.Reason == "opt-out" && r.Count == 1
            );
            Assert.Contains(
                result.ExcludedReasons,
                r => r.Reason == "invalid-contact" && r.Count == 1
            );
        }

        [Fact]
        public async Task EvaluateAsync_DoesNotInventSoftLockOrSuppressionReasons()
        {
            var seeded = await SeedLocationAsync();
            await AddGuestAsync(
                seeded,
                "Opted Out Only",
                email: "out@example.com",
                mobile: null,
                offersOptOut: true
            );

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "all-eligible-guests"
            );

            Assert.All(
                result.ExcludedReasons,
                reason =>
                {
                    Assert.DoesNotContain(
                        reason.Reason,
                        new[] { "soft-lock", "account", "suppression" }
                    );
                }
            );
            Assert.Equal("opt-out", result.ExcludedReasons.Single().Reason);
        }

        [Theory]
        [InlineData("offer-not-redeemed")]
        [InlineData("recent-redeemers")]
        [InlineData("no-recent-tummly-activity")]
        [InlineData("saved-group")]
        public async Task EvaluateAsync_UnevaluableAudiences_ReturnHonestUnavailable(
            string audienceKey
        )
        {
            var seeded = await SeedLocationAsync();
            await AddGuestAsync(
                seeded,
                "Anyone",
                email: "a@example.com",
                mobile: null,
                offersOptOut: false
            );

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                audienceKey
            );

            Assert.False(result.Evaluable);
            Assert.Null(result.Matched);
            Assert.Null(result.CurrentlyEligible);
            Assert.Null(result.Excluded);
            Assert.Null(result.EmailEligible);
            Assert.Null(result.SmsEligible);
            Assert.Empty(result.ExcludedReasons);
        }

        [Fact]
        public async Task EvaluateAsync_NewGuests_UsesSmartGroupMembershipWindow()
        {
            var seeded = await SeedLocationAsync();
            var now = DateTime.UtcNow;
            await AddGuestAsync(
                seeded,
                "Recent",
                email: "recent@example.com",
                mobile: null,
                offersOptOut: false,
                createdAt: now.AddDays(-2)
            );
            await AddGuestAsync(
                seeded,
                "Old",
                email: "old@example.com",
                mobile: null,
                offersOptOut: false,
                createdAt: now.AddDays(-(CampaignEligibilityService.NewGuestDays + 2))
            );

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "new-guests"
            );

            Assert.True(result.Evaluable);
            Assert.Equal(1, result.Matched);
            Assert.Equal(1, result.CurrentlyEligible);
            Assert.Equal(0, result.Excluded);
        }

        [Fact]
        public async Task EvaluateAsync_CompletedRecoveryFollowUp_MatchesRecoveryCompletions()
        {
            var seeded = await SeedLocationAsync();
            var recovered = await AddGuestAsync(
                seeded,
                "Recovered",
                email: "recovered@example.com",
                mobile: null,
                offersOptOut: false
            );
            await AddGuestAsync(
                seeded,
                "Other",
                email: "other@example.com",
                mobile: null,
                offersOptOut: false
            );

            var feedback = new Feedback
            {
                RestaurantLocationId = seeded.LocationId,
                QrCodeId = seeded.QrCodeId,
                LocationGuestId = recovered.LocationGuestId,
                GuestName = "Recovered",
                GuestContact = "recovered@example.com",
                ContactType = ContactType.Email,
                Comment = "Bad meal",
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                WorkflowStatus = FeedbackWorkflowStatus.Resolved,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            var statusChange = new FeedbackWorkflowStatusChange
            {
                FeedbackId = feedback.Id,
                FromStatus = FeedbackWorkflowStatus.InProgress,
                ToStatus = FeedbackWorkflowStatus.Resolved,
                AuthorUserId = seeded.OwnerUserId,
                AuthorDisplayName = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            _context.FeedbackWorkflowStatusChanges.Add(statusChange);
            await _context.SaveChangesAsync();

            _context.FeedbackRecoveryCompletions.Add(
                new FeedbackRecoveryCompletion
                {
                    FeedbackId = feedback.Id,
                    Intent = FeedbackRecoveryIntent.RespondToGuest,
                    WorkflowStatusChangeId = statusChange.Id,
                    AuthorUserId = seeded.OwnerUserId,
                    AuthorDisplayName = "Owner",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "completed-recovery-follow-up"
            );

            Assert.True(result.Evaluable);
            Assert.Equal(1, result.Matched);
            Assert.Equal(1, result.CurrentlyEligible);
        }

        [Fact]
        public async Task EvaluateAsync_RejectsUnknownAudienceKey()
        {
            var seeded = await SeedLocationAsync();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _eligibility.EvaluateAsync(seeded.LocationId, "not-a-real-audience")
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<SeededLocation> SeedLocationAsync()
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
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
                Name = "Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var qr = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.CounterCard,
                Token = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
            };
            _context.QrCodes.Add(qr);
            await _context.SaveChangesAsync();

            return new SeededLocation(user.Id, restaurant.Id, location.Id, qr.Id);
        }

        private async Task<SeededGuest> AddGuestAsync(
            SeededLocation seeded,
            string name,
            string? email,
            string? mobile,
            bool offersOptOut,
            DateTime? createdAt = null
        )
        {
            var master = new MasterGuest
            {
                RestaurantId = seeded.RestaurantId,
                Email = email,
                NormalizedEmail = email?.Trim().ToLowerInvariant(),
                Mobile = mobile,
                NormalizedPhone = mobile,
                CreatedAt = createdAt ?? DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = seeded.LocationId,
                Name = name,
                OffersOptOut = offersOptOut,
                CreatedAt = createdAt ?? DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            return new SeededGuest(guest.Id);
        }

        private sealed record SeededLocation(
            int OwnerUserId,
            int RestaurantId,
            int LocationId,
            int QrCodeId
        );

        private sealed record SeededGuest(int LocationGuestId);
    }
}

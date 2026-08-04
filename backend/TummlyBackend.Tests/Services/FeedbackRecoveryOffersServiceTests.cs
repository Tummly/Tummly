using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class FeedbackRecoveryOffersServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public FeedbackRecoveryOffersServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task SendAndIssueAsync_RetriesRedemptionCode_WhenCollisionOccurs()
        {
            var (author, feedback) = await SeedFeedbackAsync();
            await SeedExistingOfferAsync(feedback.Id, "TUM-COLLID");

            var service = new SequencedCodeFeedbackRecoveryOffersService(
                _context,
                "TUM-COLLID",
                "TUM-FRESH1"
            );

            var result = await service.SendAndIssueAsync(
                feedback.Id,
                author.Id,
                BuildRequest()
            );

            Assert.NotNull(result);
            Assert.Equal("TUM-FRESH1", result!.RecoveryOffer.RedemptionCode);

            var offers = await _context.FeedbackRecoveryOffers
                .AsNoTracking()
                .Where(o => o.FeedbackId == feedback.Id)
                .ToListAsync();
            Assert.Equal(2, offers.Count);
            Assert.Contains(offers, o => o.RedemptionCode == "TUM-COLLID");
            Assert.Contains(offers, o => o.RedemptionCode == "TUM-FRESH1");
        }

        [Fact]
        public async Task SendAndIssueAsync_ThrowsCodeAllocationException_WhenAttemptsExhausted()
        {
            var (author, feedback) = await SeedFeedbackAsync();
            await SeedExistingOfferAsync(feedback.Id, "TUM-STUCK1");

            var service = new FixedCodeFeedbackRecoveryOffersService(
                _context,
                "TUM-STUCK1"
            );

            var exception = await Record.ExceptionAsync(() =>
                service.SendAndIssueAsync(feedback.Id, author.Id, BuildRequest())
            );

            Assert.IsType<FeedbackRecoveryOfferCodeAllocationException>(exception);
            Assert.False(
                exception is InvalidOperationException,
                "Code allocation exhaustion must not be an InvalidOperationException, " +
                    "or the generic controller catch would map it to 401 Unauthorized."
            );

            var offers = await _context.FeedbackRecoveryOffers
                .AsNoTracking()
                .Where(o => o.FeedbackId == feedback.Id)
                .ToListAsync();
            Assert.Single(offers);

            var guestResponses = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .Where(r => r.FeedbackId == feedback.Id)
                .ToListAsync();
            Assert.Single(guestResponses);
        }

        private static SendAndIssueFeedbackRecoveryOfferRequest BuildRequest()
        {
            return new SendAndIssueFeedbackRecoveryOfferRequest
            {
                Channel = "email",
                Subject = "A recovery offer from us",
                Body = "Please enjoy 20% off your next visit.",
                Intent = "respond_with_recovery_offer",
                Purpose = "include_a_recovery_offer",
                Tone = "warm_and_apologetic",
                Offer = new FeedbackRecoveryOfferPayloadDto
                {
                    OfferType = "percentage_discount",
                    Title = "20% off",
                    Description = "Thanks for your feedback — enjoy 20% off.",
                    Validity = "30_days_after_issue",
                    DiscountPercentage = 20,
                },
            };
        }

        private async Task<(User Author, Feedback Feedback)> SeedFeedbackAsync()
        {
            var user = new User
            {
                FullName = "Recovery Offer Author",
                Email = $"recovery-offer-{Guid.NewGuid()}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
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
                Name = "Recovery Offer Venue",
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
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Alex Guest",
                GuestContact = "alex@example.com",
                ContactType = ContactType.Email,
                Comment = "Food was cold",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return (user, feedback);
        }

        private async Task SeedExistingOfferAsync(
            int feedbackId,
            string redemptionCode
        )
        {
            var guestResponse = new FeedbackGuestResponse
            {
                FeedbackId = feedbackId,
                Channel = FeedbackGuestResponseChannel.Email,
                Intent = FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                MaskedDestination = "a••••@example.com",
                Subject = "Existing",
                Body = "Existing body",
                AuthorDisplayName = "Seed",
                CreatedAt = DateTime.UtcNow,
            };
            _context.FeedbackGuestResponses.Add(guestResponse);

            var offer = new FeedbackRecoveryOffer
            {
                FeedbackId = feedbackId,
                GuestResponse = guestResponse,
                OfferType = FeedbackRecoveryOfferType.PercentageDiscount,
                Title = "Existing offer",
                Description = "Existing description",
                Validity = FeedbackRecoveryOfferValidity.Days30AfterIssue,
                ExpiryAt = DateTime.UtcNow.AddDays(30),
                DiscountPercentage = 10,
                RedemptionCode = redemptionCode,
                Intent = FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                AuthorDisplayName = "Seed",
                CreatedAt = DateTime.UtcNow,
            };
            _context.FeedbackRecoveryOffers.Add(offer);

            await _context.SaveChangesAsync();
        }

        private sealed class FixedCodeFeedbackRecoveryOffersService
            : FeedbackRecoveryOffersService
        {
            private readonly string _code;

            public FixedCodeFeedbackRecoveryOffersService(
                ApplicationDbContext context,
                string code
            ) : base(context)
            {
                _code = code;
            }

            protected override string GenerateCandidateCode() => _code;
        }

        private sealed class SequencedCodeFeedbackRecoveryOffersService
            : FeedbackRecoveryOffersService
        {
            private readonly Queue<string> _codes;

            public SequencedCodeFeedbackRecoveryOffersService(
                ApplicationDbContext context,
                params string[] codes
            ) : base(context)
            {
                _codes = new Queue<string>(codes);
            }

            protected override string GenerateCandidateCode()
            {
                return _codes.Count > 0
                    ? _codes.Dequeue()
                    : Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            }
        }
    }
}

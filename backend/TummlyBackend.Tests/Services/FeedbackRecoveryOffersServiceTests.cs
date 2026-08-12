using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="IFeedbackRecoveryOffersService.SendAndIssueAsync"/> —
    /// Recovery Send hard cut to catalog Offer issue (ticket 05).
    /// </summary>
    public class FeedbackRecoveryOffersServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OfferIssueService _offerIssues;
        private readonly FeedbackRecoveryOffersService _service;
        private readonly DateTime _now = new(2026, 8, 12, 15, 0, 0, DateTimeKind.Utc);

        public FeedbackRecoveryOffersServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _offerIssues = new OfferIssueService(_context);
            _service = new FeedbackRecoveryOffersService(
                _context,
                NoOpGuestResponseEmailDeliveryWork.Instance,
                _offerIssues
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task SendAndIssueAsync_WithDurableAttach_CreatesOfferIssue_NoOneOffRow()
        {
            var seeded = await SeedFeedbackWithAttachAsync();

            var result = await _service.SendAndIssueAsync(
                seeded.Feedback.Id,
                seeded.Author.Id,
                BuildRequest()
            );

            Assert.NotNull(result);
            Assert.Equal("in_progress", result!.WorkflowStatus);
            Assert.StartsWith("TUM-", result.RecoveryOffer.RedemptionCode);
            Assert.Equal("10% off next visit", result.RecoveryOffer.Title);
            Assert.Equal(
                "percentage_discount",
                result.RecoveryOffer.OfferType
            );

            Assert.Equal(0, await _context.FeedbackRecoveryOffers.CountAsync());
            Assert.Equal(1, await _context.FeedbackGuestResponses.CountAsync());
            Assert.Equal(1, await _context.OfferIssues.CountAsync());

            var issue = await _context.OfferIssues.AsNoTracking().SingleAsync();
            Assert.Equal(OfferIssueSources.Recovery, issue.Source);
            Assert.Equal(seeded.Feedback.Id, issue.FeedbackId);
            Assert.Equal(seeded.LocationGuestId, issue.LocationGuestId);
            Assert.Equal(seeded.CatalogOfferId, issue.CatalogOfferId);
            Assert.Equal(result.RecoveryOffer.RedemptionCode, issue.ClaimCode);

            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.Feedback.Id);
            Assert.Equal(FeedbackWorkflowStatus.InProgress, feedback.WorkflowStatus);
        }

        [Fact]
        public async Task SendAndIssueAsync_Sms_AppendsClaimCodeText_NoOneOffRow()
        {
            var seeded = await SeedFeedbackWithAttachAsync(
                contactType: ContactType.Phone,
                guestContact: "+447700900123"
            );

            var result = await _service.SendAndIssueAsync(
                seeded.Feedback.Id,
                seeded.Author.Id,
                BuildRequest(channel: "sms", subject: null)
            );

            Assert.NotNull(result);
            Assert.Equal(0, await _context.FeedbackRecoveryOffers.CountAsync());
            Assert.Equal(1, await _context.OfferIssues.CountAsync());

            var claimCode = result!.RecoveryOffer.RedemptionCode;
            Assert.Contains(claimCode, result.GuestResponse.Body);
            var guestResponse = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync();
            Assert.Contains(claimCode, guestResponse.Body);
            Assert.Equal(
                FeedbackGuestResponseChannel.Sms,
                guestResponse.Channel
            );
        }

        [Fact]
        public async Task SendAndIssueAsync_Throws_WhenNoCatalogAttach()
        {
            var seeded = await SeedFeedbackWithAttachAsync(attachOffer: false);

            var exception = await Record.ExceptionAsync(() =>
                _service.SendAndIssueAsync(
                    seeded.Feedback.Id,
                    seeded.Author.Id,
                    BuildRequest()
                )
            );

            Assert.IsType<ArgumentException>(exception);
            Assert.Equal(0, await _context.OfferIssues.CountAsync());
            Assert.Equal(0, await _context.FeedbackRecoveryOffers.CountAsync());
            Assert.Equal(0, await _context.FeedbackGuestResponses.CountAsync());
        }

        [Fact]
        public async Task SendAndIssueAsync_Throws_WhenGuestOffersOptOut()
        {
            var seeded = await SeedFeedbackWithAttachAsync(offersOptOut: true);

            var exception = await Record.ExceptionAsync(() =>
                _service.SendAndIssueAsync(
                    seeded.Feedback.Id,
                    seeded.Author.Id,
                    BuildRequest()
                )
            );

            Assert.IsType<ArgumentException>(exception);
            Assert.Contains("opted out", exception!.Message);
            Assert.Equal(0, await _context.OfferIssues.CountAsync());
            Assert.Equal(0, await _context.FeedbackGuestResponses.CountAsync());
        }

        [Fact]
        public async Task SendAndIssueAsync_IgnoresClientOfferPayload_UsesCatalogAttach()
        {
            var seeded = await SeedFeedbackWithAttachAsync();

            var result = await _service.SendAndIssueAsync(
                seeded.Feedback.Id,
                seeded.Author.Id,
                new SendAndIssueFeedbackRecoveryOfferRequest
                {
                    Channel = "email",
                    Subject = "A recovery offer from us",
                    Body = "Please enjoy this offer.",
                    Intent = "respond_with_recovery_offer",
                    Purpose = "include_a_recovery_offer",
                    Tone = "warm_and_apologetic",
                    Offer = new FeedbackRecoveryOfferPayloadDto
                    {
                        OfferType = "percentage_discount",
                        Title = "CLIENT PAYLOAD TITLE",
                        Description = "Should be ignored",
                        Validity = "30_days_after_issue",
                        DiscountPercentage = 99,
                    },
                }
            );

            Assert.NotNull(result);
            Assert.Equal("10% off next visit", result!.RecoveryOffer.Title);
            Assert.DoesNotContain(
                "CLIENT PAYLOAD TITLE",
                result.RecoveryOffer.Title
            );
        }

        private static SendAndIssueFeedbackRecoveryOfferRequest BuildRequest(
            string channel = "email",
            string? subject = "A recovery offer from us"
        )
        {
            return new SendAndIssueFeedbackRecoveryOfferRequest
            {
                Channel = channel,
                Subject = subject,
                Body = "Please enjoy 10% off your next visit.",
                Intent = "respond_with_recovery_offer",
                Purpose = "include_a_recovery_offer",
                Tone = "warm_and_apologetic",
            };
        }

        private async Task<(
            User Author,
            Feedback Feedback,
            int LocationGuestId,
            int CatalogOfferId
        )> SeedFeedbackWithAttachAsync(
            bool attachOffer = true,
            bool offersOptOut = false,
            ContactType contactType = ContactType.Email,
            string guestContact = "alex@example.com"
        )
        {
            var user = new User
            {
                FullName = "Recovery Offer Author",
                Email = $"recovery-offer-{Guid.NewGuid()}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
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
                Name = "Recovery Offer Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
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

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = contactType == ContactType.Email
                    ? guestContact.ToLowerInvariant()
                    : null,
                Mobile = contactType == ContactType.Phone ? guestContact : null,
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = master.Id,
                Name = "Alex Guest",
                OffersOptOut = offersOptOut,
                CreatedAt = _now,
            };
            _context.LocationGuests.Add(locationGuest);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = "active",
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off next visit",
                Description = "Come back soon",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest.Id,
                RecoveryOfferId = attachOffer ? offer.Id : null,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Food was cold",
                CreatedAt = _now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return (user, feedback, locationGuest.Id, offer.Id);
        }

        private sealed class NoOpGuestResponseEmailDeliveryWork
            : IGuestResponseEmailDeliveryWork
        {
            public static NoOpGuestResponseEmailDeliveryWork Instance { get; } =
                new();

            public ValueTask NotifyAsync(
                int guestResponseId,
                CancellationToken cancellationToken = default
            ) => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}

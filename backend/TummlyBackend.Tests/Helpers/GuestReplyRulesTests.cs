using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class GuestReplyRulesTests : IDisposable
    {
        private const string ClaimLine = "Offer claim code: TUM-ABC123";

        private readonly ApplicationDbContext _context;
        private readonly DateTime _now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

        public GuestReplyRulesTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public void ComparableStoredBody_SmsOfferRow_StripsAppendedClaimCodeLine()
        {
            var comparable = GuestReplyRules.ComparableStoredBody(
                "Hi\n\n" + ClaimLine,
                FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                FeedbackGuestResponseChannel.Sms
            );

            Assert.Equal("Hi", comparable);
        }

        [Fact]
        public void ComparableStoredBody_PlainSmsReply_KeepsClaimCodeLine()
        {
            var comparable = GuestReplyRules.ComparableStoredBody(
                "Hi\n\n" + ClaimLine,
                FeedbackRecoveryIntent.RespondToGuest,
                FeedbackGuestResponseChannel.Sms
            );

            Assert.Equal("Hi Offer claim code: TUM-ABC123", comparable);
        }

        [Fact]
        public void ComparableStoredBody_BodyWithClaimCodeAndNoSuffix_IsUnchanged()
        {
            var comparable = GuestReplyRules.ComparableStoredBody(
                "Hi TUM-ABC123",
                FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                FeedbackGuestResponseChannel.Sms
            );

            Assert.Equal("Hi TUM-ABC123", comparable);
        }

        [Fact]
        public void ComparableStoredBody_EmailOfferRow_IsNotStripped()
        {
            var comparable = GuestReplyRules.ComparableStoredBody(
                "Hi\n\n" + ClaimLine,
                FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                FeedbackGuestResponseChannel.Email
            );

            Assert.Equal("Hi Offer claim code: TUM-ABC123", comparable);
        }

        [Fact]
        public async Task EnsureNotDuplicate_SmsOfferRow_MatchesOperatorRepeatWithoutClaimLine()
        {
            SeedGuestResponse(
                FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                FeedbackGuestResponseChannel.Sms,
                "Hi\n\n" + ClaimLine
            );

            await Assert.ThrowsAsync<GuestReplyDuplicateException>(() =>
                GuestReplyRules.EnsureNotDuplicateAsync(
                    _context,
                    feedbackId: 1,
                    FeedbackGuestResponseChannel.Sms,
                    "Hi",
                    _now
                )
            );
        }

        [Fact]
        public async Task EnsureNotDuplicate_PlainSmsRowWithClaimLine_DoesNotMatchRepeatWithoutIt()
        {
            SeedGuestResponse(
                FeedbackRecoveryIntent.RespondToGuest,
                FeedbackGuestResponseChannel.Sms,
                "Hi\n\n" + ClaimLine
            );

            await GuestReplyRules.EnsureNotDuplicateAsync(
                _context,
                feedbackId: 1,
                FeedbackGuestResponseChannel.Sms,
                "Hi",
                _now
            );
        }

        [Fact]
        public async Task EnsureNotDuplicate_BodyWithClaimCodeAndNoSuffix_MatchesRepeat()
        {
            SeedGuestResponse(
                FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                FeedbackGuestResponseChannel.Sms,
                "Hi TUM-ABC123"
            );

            await Assert.ThrowsAsync<GuestReplyDuplicateException>(() =>
                GuestReplyRules.EnsureNotDuplicateAsync(
                    _context,
                    feedbackId: 1,
                    FeedbackGuestResponseChannel.Sms,
                    "  Hi   TUM-ABC123 ",
                    _now
                )
            );
        }

        [Fact]
        public async Task EnsureNoOpenRecoveryOffer_VoidedIssue_IsNotOpen()
        {
            SeedRecoveryIssue(cancelledAtUtc: _now, expiryAtUtc: _now.AddDays(30));

            await GuestReplyRules.EnsureNoOpenRecoveryOfferAsync(
                _context,
                feedbackId: 1,
                _now
            );
        }

        [Fact]
        public async Task EnsureNoOpenRecoveryOffer_OpenIssue_Throws()
        {
            SeedRecoveryIssue(cancelledAtUtc: null, expiryAtUtc: _now.AddDays(30));

            var exception = await Assert.ThrowsAsync<RecoveryOfferOpenException>(() =>
                GuestReplyRules.EnsureNoOpenRecoveryOfferAsync(
                    _context,
                    feedbackId: 1,
                    _now
                )
            );
            Assert.Contains("TUM-ABC123", exception.Message);
        }

        [Fact]
        public async Task EnsureNotDuplicate_FailedRow_DoesNotBlockRetry()
        {
            SeedGuestResponse(
                FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                FeedbackGuestResponseChannel.Sms,
                "Hi\n\n" + ClaimLine,
                GuestReplyDeliveryState.Failed
            );

            await GuestReplyRules.EnsureNotDuplicateAsync(
                _context,
                feedbackId: 1,
                FeedbackGuestResponseChannel.Sms,
                "Hi",
                _now
            );
        }

        [Theory]
        [InlineData(GuestReplyDeliveryState.Pending)]
        [InlineData(GuestReplyDeliveryState.Accepted)]
        [InlineData(GuestReplyDeliveryState.Delivered)]
        [InlineData(GuestReplyDeliveryState.Unavailable)]
        public async Task EnsureNotDuplicate_SentStates_StillBlockRepeat(
            GuestReplyDeliveryState state
        )
        {
            SeedGuestResponse(
                FeedbackRecoveryIntent.RespondToGuest,
                FeedbackGuestResponseChannel.Sms,
                "Hi",
                state
            );

            await Assert.ThrowsAsync<GuestReplyDuplicateException>(() =>
                GuestReplyRules.EnsureNotDuplicateAsync(
                    _context,
                    feedbackId: 1,
                    FeedbackGuestResponseChannel.Sms,
                    "Hi",
                    _now
                )
            );
        }

        private void SeedGuestResponse(
            FeedbackRecoveryIntent intent,
            FeedbackGuestResponseChannel channel,
            string body,
            GuestReplyDeliveryState deliveryState = GuestReplyDeliveryState.Pending
        )
        {
            _context.FeedbackGuestResponses.Add(
                new FeedbackGuestResponse
                {
                    FeedbackId = 1,
                    Channel = channel,
                    Intent = intent,
                    Body = body,
                    MaskedDestination = "a••@example.com",
                    AuthorDisplayName = "Owner",
                    CreatedAt = _now.AddMinutes(-1),
                    DeliveryState = deliveryState,
                }
            );
            _context.SaveChanges();
        }

        private void SeedRecoveryIssue(DateTime? cancelledAtUtc, DateTime expiryAtUtc)
        {
            _context.OfferIssues.Add(
                new OfferIssue
                {
                    CatalogOfferId = 1,
                    ClaimCode = "TUM-ABC123",
                    IssuedAtUtc = _now.AddDays(-1),
                    Source = OfferIssueSources.Recovery,
                    FeedbackId = 1,
                    ExpiryAtUtc = expiryAtUtc,
                    CancelledAtUtc = cancelledAtUtc,
                    Title = "20% off next visit",
                    Description = "Recovery offer",
                }
            );
            _context.SaveChanges();
        }
    }
}

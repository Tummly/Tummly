using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// A reply row whose send failed (Failed) is not a sent reply. The completion
    /// check and the activity history must not count it as sent.
    /// </summary>
    public class GuestReplyFailedStateReadersTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public GuestReplyFailedStateReadersTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task RecoveryCompletion_OnlyFailedReply_NotComplete()
        {
            var seeded = await SeedFeedbackAsync();
            await AddReplyAsync(seeded.FeedbackId, GuestReplyDeliveryState.Failed);
            var service = new FeedbackRecoveryCompletionsService(_context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CompleteAsync(
                    seeded.FeedbackId,
                    seeded.UserId,
                    FeedbackRecoveryIntent.RespondToGuest
                )
            );
        }

        [Fact]
        public async Task RecoveryCompletion_AcceptedReply_Completes()
        {
            var seeded = await SeedFeedbackAsync();
            await AddReplyAsync(seeded.FeedbackId, GuestReplyDeliveryState.Accepted);
            var service = new FeedbackRecoveryCompletionsService(_context);

            var result = await service.CompleteAsync(
                seeded.FeedbackId,
                seeded.UserId,
                FeedbackRecoveryIntent.RespondToGuest
            );

            Assert.NotNull(result);
        }

        [Fact]
        public void ActivityHistory_FailedReply_NoSentEvent()
        {
            var at = new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc);
            var history = FeedbackActivityHistory.Derive(
                at.AddHours(-1),
                Array.Empty<FeedbackInternalNoteActivityFactDto>(),
                guestResponsesNewestFirst: new[]
                {
                    Reply(2, "failed", at.AddMinutes(5)),
                    Reply(1, "accepted", at),
                }
            );

            var sent = Assert.Single(history, e => e.Kind == "guest_response_sent");
            Assert.Equal(1, sent.GuestResponseId);

            // The failed reply stays in the Service reply history, as "not sent".
            var notSent = Assert.Single(history, e => e.Kind == "guest_response_not_sent");
            Assert.Equal(2, notSent.GuestResponseId);
            Assert.Equal("failed", notSent.DeliveryState);
        }

        private static FeedbackGuestResponseItemDto Reply(int id, string state, DateTime at) =>
            new()
            {
                Id = id,
                Channel = "sms",
                Intent = "respond_to_guest",
                MaskedDestination = "+44••••3491",
                Body = "Thanks.",
                AuthorDisplayName = "Operator",
                CreatedAt = at,
                DeliveryState = state,
                DeliveryStateAt = at,
            };

        private async Task AddReplyAsync(int feedbackId, GuestReplyDeliveryState state)
        {
            _context.FeedbackGuestResponses.Add(
                new FeedbackGuestResponse
                {
                    FeedbackId = feedbackId,
                    Channel = FeedbackGuestResponseChannel.Sms,
                    Intent = FeedbackRecoveryIntent.RespondToGuest,
                    MaskedDestination = "+44••••3491",
                    Body = "Thanks.",
                    AuthorDisplayName = "Operator",
                    CreatedAt = DateTime.UtcNow,
                    DeliveryState = state,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task<(int UserId, int FeedbackId)> SeedFeedbackAsync()
        {
            var user = new User
            {
                FullName = "Completer",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Completion Venue",
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
                GuestName = "Pat Guest",
                GuestContact = "+447911123491",
                ContactType = ContactType.Phone,
                Comment = "Cold food",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Pending,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return (user.Id, feedback.Id);
        }
    }
}

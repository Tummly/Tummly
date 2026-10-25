using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ILocationGuestDeleteService"/> — owns Owned-location
    /// authz and Location Guest hard-delete policy.
    /// </summary>
    public class LocationGuestDeleteServiceTests : IDisposable
    {
        private static readonly DateTime T1 = new(
            2026,
            3,
            1,
            10,
            0,
            0,
            DateTimeKind.Utc
        );

        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestDeleteService _delete;
        private readonly GuestContactHasher _hasher;

        public LocationGuestDeleteServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _hasher = new GuestContactHasher("test-key");
            var ownedLocation = new OwnedLocationService(_context);
            var audit = new AdminAuditService(_context, TimeProvider.System);
            var suppressions = new GuestContactSuppressionService(
                _context,
                _hasher
            );
            _delete = new LocationGuestDeleteService(
                _context,
                ownedLocation,
                audit,
                suppressions
            );
        }

        [Fact]
        public async Task DeleteAsync_ReturnsForbidden_WhenUserDoesNotOwnLocation()
        {
            var seeded = await SeedOwnedGuestAsync();
            var otherUserId = seeded.OwnerUserId + 999;

            var outcome = await _delete.DeleteAsync(
                otherUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Forbidden, outcome.Status);
            Assert.Equal(
                "You do not have access to this location.",
                outcome.ErrorMessage
            );
            Assert.True(
                await _context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenLocationMissing()
        {
            var seeded = await SeedOwnedGuestAsync();

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                locationId: 999_999
            );

            Assert.Equal(LocationGuestDeleteStatus.NotFound, outcome.Status);
            Assert.Equal("Location not found.", outcome.ErrorMessage);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenGuestMissingOrWrongLocation()
        {
            var seeded = await SeedOwnedGuestAsync();

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                locationGuestId: 999_999,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.NotFound, outcome.Status);
            Assert.Equal("Guest not found.", outcome.ErrorMessage);
        }

        [Fact]
        public async Task DeleteAsync_Deletes_WhenOwned()
        {
            var seeded = await SeedOwnedGuestAsync();

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);
            Assert.False(
                await _context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );
        }

        [Fact]
        public async Task DeleteAsync_LeavesAssistantQuotes_Unchanged()
        {
            var seeded = await SeedOwnedGuestAsync();
            const string excerpt = "Slow service at dinner";
            const string body = "Pat said: Slow service at dinner";

            var conversation = new AssistantConversation
            {
                OwnerUserId = seeded.OwnerUserId,
                Title = "Guest quote",
                OwnedLocationId = seeded.LocationId,
                OwnedLocationName = "Main",
                ReportingPeriodKind = "preset",
                ReportingPeriodPresetId = "last7",
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
            };
            conversation.Messages.Add(
                new AssistantMessage
                {
                    Role = AssistantMessageRole.User,
                    Body = "Summarise Pat",
                    CreatedAt = DateTime.UtcNow,
                }
            );
            conversation.Messages.Add(
                new AssistantMessage
                {
                    Role = AssistantMessageRole.Assistant,
                    Class = AssistantMessageClass.Grounded,
                    Body = body,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            _context.AssistantConversations.Add(conversation);
            await _context.SaveChangesAsync();
            var conversationId = conversation.Id;

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);
            Assert.False(
                await _context.LocationGuests.AnyAsync(
                    lg => lg.Id == seeded.LocationGuestId
                )
            );

            var stored = await _context.AssistantConversations
                .Include(row => row.Messages)
                .SingleAsync(row => row.Id == conversationId);
            var assistantBody = stored.Messages
                .Single(message => message.Role == AssistantMessageRole.Assistant)
                .Body;
            Assert.Contains("Pat", assistantBody);
            Assert.Contains(excerpt, assistantBody);
            Assert.Equal(body, assistantBody);
        }

        [Fact]
        public async Task Delete_UnlinksClaimsAndCampaignRows()
        {
            var seeded = await SeedOwnedGuestAsync();
            var claimId = await SeedClaimAsync(seeded);
            var (frozenId, deliveryId) = await SeedCampaignRowsAsync(seeded);

            // Detach seeded dependents so unlink must be explicit (not EF fixup).
            foreach (var entry in _context.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);

            var claim = await _context.OfferIssues.SingleAsync(o => o.Id == claimId);
            Assert.Null(claim.LocationGuestId);

            var frozen = await _context.CampaignFrozenRecipients.SingleAsync(
                r => r.Id == frozenId
            );
            Assert.Null(frozen.LocationGuestId);

            var delivery = await _context.CampaignRecipientDeliveries.SingleAsync(
                d => d.Id == deliveryId
            );
            Assert.Null(delivery.LocationGuestId);
        }

        [Fact]
        public async Task Delete_AnonymisesFeedback_KeepsComment()
        {
            var seeded = await SeedOwnedGuestAsync();
            const string comment = "Service was slow but food was great";
            var feedbackId = await SeedFeedbackAsync(
                seeded,
                guestName: "Pat",
                guestContact: "pat@example.com",
                contactType: ContactType.Email,
                comment: comment,
                sentiment: FeedbackSentiment.Negative,
                workflowStatus: FeedbackWorkflowStatus.InProgress
            );

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);

            var feedback = await _context.Feedbacks.SingleAsync(
                f => f.Id == feedbackId
            );
            Assert.Null(feedback.LocationGuestId);
            Assert.Equal(DeletedGuestDisplay.Name, feedback.GuestName);
            Assert.Equal(string.Empty, feedback.GuestContact);
            Assert.Equal(ContactType.Unknown, feedback.ContactType);
            Assert.Equal(comment, feedback.Comment);
            Assert.Equal(FeedbackSentiment.Negative, feedback.Sentiment);
            Assert.Equal(
                FeedbackWorkflowStatus.InProgress,
                feedback.WorkflowStatus
            );
        }

        [Fact]
        public async Task Delete_WritesSuppressionOnlyForWithdrawnKinds()
        {
            var seeded = await SeedOwnedGuestAsync();
            await SeedLedgerAsync(
                seeded,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                T1
            );
            await SeedLedgerAsync(
                seeded,
                LocationGuestPermissionKind.FeedbackFollowUp,
                LocationGuestPermissionLedgerEventKinds.Grant,
                T1.AddHours(1)
            );

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);

            var rows = await _context.GuestContactSuppressions.ToListAsync();
            var row = Assert.Single(rows);
            Assert.Equal(
                LocationGuestPermissionKind.EmailMarketing,
                row.PermissionKind
            );
            Assert.Equal(T1, row.WithdrawnAtUtc);
            Assert.Equal(GuestContactSuppressionSources.GuestDelete, row.Source);
            Assert.Equal(seeded.RestaurantId, row.RestaurantId);
            Assert.Equal(
                _hasher.Hash("email:pat@example.com"),
                row.ContactHash
            );
        }

        [Fact]
        public async Task Delete_LegacyOptedOutWithoutLedger_UsesDeleteTime()
        {
            var seeded = await SeedOwnedGuestAsync(
                marketingPreference: LocationGuestMarketingPreference.OptedOut
            );

            var before = DateTime.UtcNow;
            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );
            var after = DateTime.UtcNow;

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);

            var rows = await _context.GuestContactSuppressions.ToListAsync();
            Assert.NotEmpty(rows);
            foreach (var row in rows)
            {
                Assert.Equal(
                    GuestContactSuppressionSources.GuestDelete,
                    row.Source
                );
                Assert.InRange(row.WithdrawnAtUtc, before, after);
            }

            Assert.Contains(
                rows,
                r =>
                    r.PermissionKind
                    == LocationGuestPermissionKind.EmailMarketing
            );
            Assert.Contains(
                rows,
                r =>
                    r.PermissionKind == LocationGuestPermissionKind.SmsMarketing
            );
            Assert.Contains(
                rows,
                r =>
                    r.PermissionKind
                    == LocationGuestPermissionKind.FeedbackFollowUp
            );
        }

        [Fact]
        public async Task Delete_WritesAuditWithoutContact()
        {
            var seeded = await SeedOwnedGuestAsync();
            await SeedClaimAsync(seeded);
            await SeedCampaignRowsAsync(seeded);
            await SeedFeedbackAsync(
                seeded,
                guestName: "Pat",
                guestContact: "pat@example.com",
                contactType: ContactType.Email,
                comment: "Keep me",
                sentiment: FeedbackSentiment.Positive,
                workflowStatus: FeedbackWorkflowStatus.New
            );

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );

            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);

            var audit = Assert.Single(
                await _context.AdminAuditEvents.ToListAsync()
            );
            Assert.Equal(AdminAuditActions.GuestDelete, audit.Action);
            Assert.Equal(
                AdminAuditTargetTypes.LocationGuest,
                audit.TargetType
            );
            Assert.Equal(
                seeded.LocationGuestId.ToString(),
                audit.TargetId
            );
            Assert.Equal($"user:{seeded.OwnerUserId}", audit.ActorIdentity);
            Assert.Equal(seeded.RestaurantId, audit.RestaurantId);

            Assert.False(string.IsNullOrWhiteSpace(audit.DetailJson));
            using var doc = JsonDocument.Parse(audit.DetailJson!);
            var root = doc.RootElement;
            Assert.Equal(seeded.LocationId, root.GetProperty("locationId").GetInt32());
            Assert.Equal(1, root.GetProperty("feedbacks").GetInt32());
            Assert.Equal(1, root.GetProperty("claims").GetInt32());
            Assert.Equal(1, root.GetProperty("frozenRecipients").GetInt32());
            Assert.Equal(1, root.GetProperty("deliveries").GetInt32());

            Assert.DoesNotContain(
                "pat@example.com",
                audit.DetailJson!,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain(
                "Pat",
                audit.DetailJson!,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task Respond_DeletedGuestFeedback_Rejected()
        {
            var seeded = await SeedOwnedGuestAsync();
            await SeedLedgerAsync(
                seeded,
                LocationGuestPermissionKind.FeedbackFollowUp,
                LocationGuestPermissionLedgerEventKinds.Grant,
                T1
            );
            var feedbackId = await SeedFeedbackAsync(
                seeded,
                guestName: "Pat",
                guestContact: "pat@example.com",
                contactType: ContactType.Email,
                comment: "Please reply",
                sentiment: FeedbackSentiment.Negative,
                workflowStatus: FeedbackWorkflowStatus.InProgress
            );

            var outcome = await _delete.DeleteAsync(
                seeded.OwnerUserId,
                seeded.LocationGuestId,
                seeded.LocationId
            );
            Assert.Equal(LocationGuestDeleteStatus.Deleted, outcome.Status);

            var feedback = await _context.Feedbacks.SingleAsync(
                f => f.Id == feedbackId
            );
            Assert.Equal(string.Empty, feedback.GuestContact);
            Assert.Equal(ContactType.Unknown, feedback.ContactType);

            Assert.False(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    feedback,
                    FeedbackGuestResponseChannel.Email,
                    CancellationToken.None
                )
            );
            Assert.False(
                await ServiceReplyEligibility.IsAllowedAsync(
                    _context,
                    feedback,
                    FeedbackGuestResponseChannel.Sms,
                    CancellationToken.None
                )
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task SeedLedgerAsync(
            SeededGuest seeded,
            LocationGuestPermissionKind kind,
            string eventKind,
            DateTime occurredAt
        )
        {
            _context.LocationGuestPermissionLedgerEntries.Add(
                new LocationGuestPermissionLedgerEntry
                {
                    LocationGuestId = seeded.LocationGuestId,
                    RestaurantLocationId = seeded.LocationId,
                    PermissionKind = kind,
                    EventKind = eventKind,
                    Source = LocationGuestPermissionLedgerSources.GuestForm,
                    OccurredAt = occurredAt,
                    CreatedAt = occurredAt,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task<int> SeedFeedbackAsync(
            SeededGuest seeded,
            string guestName,
            string guestContact,
            ContactType contactType,
            string comment,
            FeedbackSentiment sentiment,
            FeedbackWorkflowStatus workflowStatus
        )
        {
            var feedback = new Feedback
            {
                RestaurantLocationId = seeded.LocationId,
                LocationGuestId = seeded.LocationGuestId,
                GuestName = guestName,
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = comment,
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = sentiment,
                DetectedTagsJson = "[]",
                WorkflowStatus = workflowStatus,
            };
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return feedback.Id;
        }

        private async Task<int> SeedClaimAsync(SeededGuest seeded)
        {
            var offer = new CatalogOffer
            {
                RestaurantLocationId = seeded.LocationId,
                Status = "active",
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off",
                Description = "desc",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            var issue = new OfferIssue
            {
                CatalogOfferId = offer.Id,
                LocationGuestId = seeded.LocationGuestId,
                ClaimCode = $"TUM-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                IssuedAtUtc = DateTime.UtcNow,
                ClaimedAtUtc = DateTime.UtcNow,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = DateTime.UtcNow.AddDays(7),
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off",
                Description = "desc",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
            };
            _context.OfferIssues.Add(issue);
            await _context.SaveChangesAsync();
            return issue.Id;
        }

        private async Task<(int FrozenId, int DeliveryId)> SeedCampaignRowsAsync(
            SeededGuest seeded
        )
        {
            var campaign = new Campaign
            {
                RestaurantLocationId = seeded.LocationId,
                Status = "sending",
                Name = "Delete unlink campaign",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Hi",
                MessageBody = "Body",
                ScheduleMode = "send-now",
                ScheduleTimeZone = "Europe/London",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            var frozen = new CampaignFrozenRecipient
            {
                CampaignId = campaign.Id,
                LocationGuestId = seeded.LocationGuestId,
                FrozenAtUtc = DateTime.UtcNow,
            };
            var delivery = new CampaignRecipientDelivery
            {
                CampaignId = campaign.Id,
                LocationGuestId = seeded.LocationGuestId,
                Channel = "email",
                Outcome = "accepted",
                AcceptedAtUtc = DateTime.UtcNow,
                AcceptedUnits = 1,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            _context.CampaignFrozenRecipients.Add(frozen);
            _context.CampaignRecipientDeliveries.Add(delivery);
            await _context.SaveChangesAsync();
            return (frozen.Id, delivery.Id);
        }

        private async Task<SeededGuest> SeedOwnedGuestAsync(
            LocationGuestMarketingPreference marketingPreference =
                LocationGuestMarketingPreference.Allowed
        )
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

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "pat@example.com",
                NormalizedEmail = "pat@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Pat",
                MarketingPreference = marketingPreference,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            return new SeededGuest(
                user.Id,
                restaurant.Id,
                location.Id,
                guest.Id
            );
        }

        private sealed record SeededGuest(
            int OwnerUserId,
            int RestaurantId,
            int LocationId,
            int LocationGuestId
        );
    }
}

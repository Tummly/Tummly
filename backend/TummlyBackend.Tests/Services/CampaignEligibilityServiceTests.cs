using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
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
                r => r.Reason == "withdrawn" && r.Count == 1
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
                        new[] { "soft-lock", "account" }
                    );
                }
            );
            Assert.Equal("withdrawn", result.ExcludedReasons.Single().Reason);
        }

        [Fact]
        public async Task EvaluateAsync_TreatsNotRecordedAsNotGrantedExclusion()
        {
            var seeded = await SeedLocationAsync();
            var guest = await AddGuestAsync(
                seeded,
                "Not Recorded",
                email: "nr@example.com",
                mobile: "07700900333",
                offersOptOut: false
            );
            var row = await _context.LocationGuests.SingleAsync(
                lg => lg.Id == guest.LocationGuestId
            );
            row.MarketingPreference = LocationGuestMarketingPreference.NotRecorded;
            await _context.SaveChangesAsync();

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "all-eligible-guests"
            );

            Assert.Equal(1, result.Matched);
            Assert.Equal(0, result.CurrentlyEligible);
            Assert.Equal(1, result.Excluded);
            Assert.Equal("not-granted", result.ExcludedReasons.Single().Reason);
        }

        [Fact]
        public async Task EvaluateAsync_EmailOnlyGuestWithEmailRestaurantOff_IsSuppressed()
        {
            var seeded = await SeedLocationAsync();
            var restaurant = await _context.Restaurants.SingleAsync(
                r => r.Id == seeded.RestaurantId
            );
            restaurant.EmailMarketingPermissionEnabled = false;
            restaurant.SmsMarketingPermissionEnabled = true;
            await _context.SaveChangesAsync();

            await AddGuestAsync(
                seeded,
                "Email Only",
                email: "email-only@example.com",
                mobile: null,
                offersOptOut: false
            );

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "all-eligible-guests"
            );

            Assert.Equal(1, result.Matched);
            Assert.Equal(0, result.CurrentlyEligible);
            Assert.Equal("suppressed", result.ExcludedReasons.Single().Reason);
        }

        [Fact]
        public async Task EvaluateAsync_EmailHardBounce_ExcludesEmailMarketingWithoutLedgerWithdrawal()
        {
            var seeded = await SeedLocationAsync();
            var restaurant = await _context.Restaurants.SingleAsync(r => r.Id == seeded.RestaurantId);
            restaurant.EmailMarketingPermissionEnabled = true;
            restaurant.SmsMarketingPermissionEnabled = true;
            await _context.SaveChangesAsync();
            await AddGuestAsync(
                seeded,
                "Bounced",
                email: "bounced@example.com",
                mobile: "+447911123456",
                offersOptOut: false
            );

            var before = await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                seeded.LocationId,
                "all-eligible-guests",
                "email"
            );
            Assert.Single(before);

            var suppressions = new GuestContactSuppressionService(
                _context,
                new GuestContactHasher(GuestContactHasher.TestingFallbackKey)
            );
            await suppressions.StageAsync(
                seeded.RestaurantId,
                "email:bounced@example.com",
                LocationGuestPermissionKind.EmailDelivery,
                DateTime.UtcNow,
                GuestContactSuppressionSources.Bounce
            );
            await _context.SaveChangesAsync();

            var emailIds = await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                seeded.LocationId,
                "all-eligible-guests",
                "email"
            );
            Assert.Empty(emailIds);

            var result = await _eligibility.EvaluateAsync(seeded.LocationId, "all-eligible-guests");
            Assert.Equal(0, result.EmailEligible);
            Assert.Equal(1, result.SmsEligible);
            Assert.Equal(1, result.CurrentlyEligible);

            // The bounce is a delivery fact: no consent withdrawal is written.
            Assert.Empty(await _context.LocationGuestPermissionLedgerEntries.ToListAsync());
        }

        [Fact]
        public async Task EvaluateAsync_EmailHardBounceWithEmailRestaurantOff_StaysSuppressed()
        {
            var seeded = await SeedLocationAsync();
            var restaurant = await _context.Restaurants.SingleAsync(r => r.Id == seeded.RestaurantId);
            restaurant.EmailMarketingPermissionEnabled = false;
            await _context.SaveChangesAsync();
            await AddGuestAsync(
                seeded,
                "Bounced Restaurant Off",
                email: "bounced-off@example.com",
                mobile: null,
                offersOptOut: false
            );

            var suppressions = new GuestContactSuppressionService(
                _context,
                new GuestContactHasher(GuestContactHasher.TestingFallbackKey)
            );
            await suppressions.StageAsync(
                seeded.RestaurantId,
                "email:bounced-off@example.com",
                LocationGuestPermissionKind.EmailDelivery,
                DateTime.UtcNow,
                GuestContactSuppressionSources.Bounce
            );
            await _context.SaveChangesAsync();

            var result = await _eligibility.EvaluateAsync(seeded.LocationId, "all-eligible-guests");

            Assert.Equal("suppressed", result.ExcludedReasons.Single().Reason);
        }

        [Fact]
        public async Task EvaluateAsync_EmailHardBounceAndNoOtherChannel_IsPreviousHardBounce()
        {
            var seeded = await SeedLocationAsync();
            var restaurant = await _context.Restaurants.SingleAsync(r => r.Id == seeded.RestaurantId);
            restaurant.EmailMarketingPermissionEnabled = true;
            await _context.SaveChangesAsync();
            await AddGuestAsync(
                seeded,
                "Bounced Only",
                email: "bounced-only@example.com",
                mobile: null,
                offersOptOut: false
            );

            var suppressions = new GuestContactSuppressionService(
                _context,
                new GuestContactHasher(GuestContactHasher.TestingFallbackKey)
            );
            await suppressions.StageAsync(
                seeded.RestaurantId,
                "email:bounced-only@example.com",
                LocationGuestPermissionKind.EmailDelivery,
                DateTime.UtcNow,
                GuestContactSuppressionSources.Bounce
            );
            await _context.SaveChangesAsync();

            var result = await _eligibility.EvaluateAsync(seeded.LocationId, "all-eligible-guests");

            Assert.Equal(0, result.CurrentlyEligible);
            Assert.Equal(1, result.Excluded);
            Assert.Equal("previous_hard_bounce", result.ExcludedReasons.Single().Reason);
        }

        [Theory]
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

        [Fact]
        public async Task CampaignEligibility_StoppedGuest_ExcludedFromSms()
        {
            var seeded = await SeedLocationAsync();
            var both = await AddGuestAsync(
                seeded,
                "Stopped Both",
                email: "stopped-both@example.com",
                mobile: "07700900444",
                offersOptOut: false
            );
            await AddGuestAsync(
                seeded,
                "Stopped Mobile Only",
                email: null,
                mobile: "07700900555",
                offersOptOut: false
            );

            var before = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "all-eligible-guests"
            );
            Assert.Equal(2, before.SmsEligible);

            var stopped = await _context.MasterGuests
                .Where(m => m.Mobile == "07700900444" || m.Mobile == "07700900555")
                .ToListAsync();
            Assert.Equal(2, stopped.Count);
            foreach (var master in stopped)
            {
                master.SmsStoppedAtUtc = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "all-eligible-guests"
            );
            Assert.Equal(2, result.Matched);
            Assert.Equal(1, result.CurrentlyEligible);
            Assert.Equal(1, result.EmailEligible);
            Assert.Equal(0, result.SmsEligible);
            Assert.Contains(
                result.ExcludedReasons,
                r => r.Reason == "withdrawn" && r.Count == 1
            );

            var smsIds = await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                seeded.LocationId,
                "all-eligible-guests",
                "sms"
            );
            Assert.Empty(smsIds);

            var emailIds = await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                seeded.LocationId,
                "all-eligible-guests",
                "email"
            );
            Assert.Equal(both.LocationGuestId, Assert.Single(emailIds));
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CustomAudience_NamesAndExclusions_RestrictActualRecipients()
        {
            var location = await SeedLocationAsync();
            var mohamed = await AddGuestAsync(location, "Mohamed", "m@example.com", null, false);
            var sophie = await AddGuestAsync(location, "Sophie", "s@example.com", null, false);
            await AddGuestAsync(location, "Other", "o@example.com", null, false);
            var key = await CampaignAudienceStore.CreateAsync(_context, location.LocationId,
                new CampaignAudienceDefinition { GuestNames = ["Mohamed", "Sophie"], ExcludedNames = ["Sophie"] }, null, default);
            var ids = await _eligibility.ListChannelEligibleLocationGuestIdsAsync(location.LocationId, key, "email");
            Assert.Equal(new[] { mohamed.LocationGuestId }, ids);
            Assert.DoesNotContain(sophie.LocationGuestId, ids);
        }

        [Fact]
        public async Task CustomAudience_LimitIsAppliedAfterConsent_AndRevalidated()
        {
            var location = await SeedLocationAsync();
            await AddGuestAsync(location, "Withdrawn", "w@example.com", null, true);
            var one = await AddGuestAsync(location, "First", "f@example.com", null, false, DateTime.UtcNow.AddDays(-1));
            await AddGuestAsync(location, "Second", "s@example.com", null, false, DateTime.UtcNow.AddDays(-2));
            var key = await CampaignAudienceStore.CreateAsync(_context, location.LocationId,
                new CampaignAudienceDefinition { Limit = 1 }, "Recent eligible", default);
            Assert.Equal(new[] { one.LocationGuestId }, await _eligibility.ListChannelEligibleLocationGuestIdsAsync(location.LocationId, key, "email"));
            var preview = await _eligibility.EvaluateAsync(location.LocationId, key);
            Assert.Equal(3, preview.Matched);
            Assert.Equal(1, preview.EmailEligible);
            var first = await _context.LocationGuests.FindAsync(one.LocationGuestId);
            first!.MarketingPreference = LocationGuestMarketingPreference.OptedOut;
            await _context.SaveChangesAsync();
            Assert.DoesNotContain(one.LocationGuestId, await _eligibility.ListChannelEligibleLocationGuestIdsAsync(location.LocationId, key, "email"));
        }

        [Fact]
        public async Task CustomAudience_CannotReadOrSelectAnotherLocationsGuests()
        {
            var first = await SeedLocationAsync();
            var second = await SeedLocationAsync();
            var guest = await AddGuestAsync(second, "Other venue", "o@example.com", null, false);
            await Assert.ThrowsAsync<ArgumentException>(() => CampaignAudienceStore.CreateAsync(_context, first.LocationId,
                new CampaignAudienceDefinition { GuestIds = [guest.LocationGuestId] }, null, default));
            var key = await CampaignAudienceStore.CreateAsync(_context, first.LocationId, new CampaignAudienceDefinition(), "Mine", default);
            await Assert.ThrowsAsync<ArgumentException>(() => _eligibility.EvaluateAsync(second.LocationId, key));
        }

        [Fact]
        public async Task CustomAudience_AmbiguousAndMissingNames_DoNotBecomeAllGuests()
        {
            var location = await SeedLocationAsync();
            await AddGuestAsync(location, "Sophie", "s1@example.com", null, false);
            await AddGuestAsync(location, "Sophie", "s2@example.com", null, false);
            foreach (var name in new[] { "Sophie", "Missing" })
                await Assert.ThrowsAsync<ArgumentException>(() => CampaignAudienceStore.CreateAsync(_context, location.LocationId,
                    new CampaignAudienceDefinition { GuestNames = [name] }, null, default));
        }

        [Fact]
        public async Task SavedAudience_CustomDateFilter_IsReusedFromStoredDefinition()
        {
            var location = await SeedLocationAsync();
            var recent = await AddGuestAsync(location, "Recent", "r@example.com", null, false);
            await AddGuestAsync(location, "Old", "o@example.com", null, false, DateTime.UtcNow.AddDays(-60));
            var key = await CampaignAudienceStore.CreateAsync(_context, location.LocationId,
                new CampaignAudienceDefinition { JoinedFromUtc = DateTime.UtcNow.AddDays(-30) }, "Last month", default);
            Assert.Equal(new[] { recent.LocationGuestId }, await _eligibility.ListChannelEligibleLocationGuestIdsAsync(location.LocationId, key, "email"));
            Assert.Equal(DateTime.UtcNow.AddDays(-30).Date,
                CampaignAudienceStore.Load(_context, location.LocationId, key).JoinedFromUtc!.Value.Date);
            Assert.Equal("Last month", (await _context.CampaignAudiences.SingleAsync()).Name);
        }

        [Theory]
        [InlineData("Draft an Email Campaign to Mohamed and Sophie only", 2)]
        [InlineData("make capaign for Mohamed and Sophie", 2)]
        [InlineData("Please create an Email Campaign for Mohamed and Sophie only.", 2)]
        [InlineData("Change the audience to Mohamed and Sophie only", 2)]
        [InlineData("Change the audience to Mohamed and Sophie only.", 2)]
        [InlineData("Draft an Email Campaign to the 1 most recent eligible guests", 1)]
        [InlineData("Draft an Email Campaign to all eligible guests excluding Sophie", 1)]
        [InlineData("Draft an Email Campaign to all eligible guests except Sophie.", 1)]
        public async Task AssistantAudience_ResolvedFieldsRestrictActualRecipients(string message, int count)
        {
            var location = await SeedLocationAsync();
            await AddGuestAsync(location, "Mohamed", "m@example.com", null, false);
            await AddGuestAsync(location, "Sophie", "s@example.com", null, false);
            var bound = Assert.IsType<TummlyBackend.Helpers.AssistantCampaignDraftBindOutcome.Bound>(
                TummlyBackend.Helpers.AssistantCampaignDraftBind.Resolve(message, "Main", [], []));
            var fields = await CampaignAudienceStore.BindAsync(_context, location.LocationId, bound.Fields, default);
            Assert.StartsWith("audience:", fields.AudienceKey);
            Assert.Equal(count, (await _eligibility.ListChannelEligibleLocationGuestIdsAsync(location.LocationId, fields.AudienceKey, "email")).Count);
        }

        [Theory]
        [InlineData("Create a campaign for Mary O’Brien")]
        [InlineData("Change the audience to Mary O’Brien only.")]
        [InlineData("Create a campaign for all eligible guests except Mohamed.")]
        public async Task PunctuatedAudience_UsesOnlyTheRequestedRecipient(string request)
        {
            var location = await SeedLocationAsync();
            var mary = await AddGuestAsync(location, "Mary O’Brien", "mary@example.com", null, false);
            await AddGuestAsync(location, "Mohamed", "m@example.com", null, false);
            var bound = Assert.IsType<TummlyBackend.Helpers.AssistantCampaignDraftBindOutcome.Bound>(
                TummlyBackend.Helpers.AssistantCampaignDraftBind.Resolve(request, "Main", [], []));
            var fields = await CampaignAudienceStore.BindAsync(_context, location.LocationId, bound.Fields, default);
            Assert.Equal(new[] { mary.LocationGuestId },
                await _eligibility.ListChannelEligibleLocationGuestIdsAsync(location.LocationId, fields.AudienceKey, "email"));
        }

        [Fact]
        public async Task EvaluateAsync_OfferNotRedeemed_IncludesClaimedNotRedeemedOnly()
        {
            var seeded = await SeedLocationAsync();
            var offerId = await AddOfferAsync(seeded.LocationId);
            var otherOfferId = await AddOfferAsync(seeded.LocationId, "Other offer");

            var claimed = await AddGuestAsync(seeded, "Claimed", "c@example.com", null, false);
            var redeemed = await AddGuestAsync(seeded, "Redeemed", "r@example.com", null, false);
            var neverReceived = await AddGuestAsync(seeded, "Never", "n@example.com", null, false);
            var otherOffer = await AddGuestAsync(seeded, "Other Offer", "o@example.com", null, false);
            var expired = await AddGuestAsync(seeded, "Expired", "e@example.com", null, false);
            var cancelled = await AddGuestAsync(seeded, "Cancelled", "x@example.com", null, false);

            await AddOfferIssueAsync(offerId, claimed.LocationGuestId);
            await AddOfferIssueAsync(offerId, redeemed.LocationGuestId, redeemedAtUtc: DateTime.UtcNow.AddDays(-2));
            await AddOfferIssueAsync(otherOfferId, otherOffer.LocationGuestId);
            await AddOfferIssueAsync(offerId, expired.LocationGuestId, expiryAtUtc: DateTime.UtcNow.AddDays(-1));
            await AddOfferIssueAsync(offerId, cancelled.LocationGuestId, cancelledAtUtc: DateTime.UtcNow.AddDays(-1));

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "offer-not-redeemed",
                new CampaignAudienceScope(OfferId: offerId)
            );

            Assert.True(result.Evaluable);
            Assert.Equal(1, result.Matched);
            Assert.Equal(1, result.CurrentlyEligible);
            Assert.Null(result.UnavailableReason);
            var ids = await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                seeded.LocationId,
                "offer-not-redeemed",
                "email",
                new CampaignAudienceScope(OfferId: offerId)
            );
            Assert.Equal(new[] { claimed.LocationGuestId }, ids);
            // neverReceived is only meaningful as a non-match above.
            Assert.DoesNotContain(neverReceived.LocationGuestId, ids);
        }

        [Fact]
        public async Task EvaluateAsync_OfferNotRedeemed_ExcludesGuestWhoRedeemedTheOfferAgain()
        {
            var seeded = await SeedLocationAsync();
            var offerId = await AddOfferAsync(seeded.LocationId);
            var guest = await AddGuestAsync(seeded, "Two Passes", "two@example.com", null, false);

            await AddOfferIssueAsync(offerId, guest.LocationGuestId, redeemedAtUtc: DateTime.UtcNow.AddDays(-1));
            await AddOfferIssueAsync(offerId, guest.LocationGuestId);

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "offer-not-redeemed",
                new CampaignAudienceScope(OfferId: offerId)
            );

            Assert.Equal(0, result.Matched);
        }

        [Fact]
        public async Task EvaluateAsync_OfferNotRedeemed_NoConsentIsMatchedButNotEligible()
        {
            var seeded = await SeedLocationAsync();
            var offerId = await AddOfferAsync(seeded.LocationId);
            var guest = await AddGuestAsync(seeded, "Opted Out", "out@example.com", null, offersOptOut: true);
            await AddOfferIssueAsync(offerId, guest.LocationGuestId);

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "offer-not-redeemed",
                new CampaignAudienceScope(OfferId: offerId)
            );

            Assert.Equal(1, result.Matched);
            Assert.Equal(0, result.CurrentlyEligible);
            Assert.Equal(1, result.Excluded);
        }

        [Fact]
        public async Task EvaluateAsync_OfferNotRedeemed_WithoutOfferIsUnavailableWithReason()
        {
            var seeded = await SeedLocationAsync();

            var result = await _eligibility.EvaluateAsync(seeded.LocationId, "offer-not-redeemed");

            Assert.False(result.Evaluable);
            Assert.Null(result.Matched);
            Assert.Equal(CampaignEligibilityService.NoOfferAttachedReason, result.UnavailableReason);
        }

        [Fact]
        public async Task EvaluateAsync_OfferNotRedeemed_OtherLocationsOfferDoesNotCount()
        {
            var seeded = await SeedLocationAsync();
            var other = await SeedLocationAsync();
            var otherLocationOffer = await AddOfferAsync(other.LocationId);
            var guest = await AddGuestAsync(seeded, "Here", "here@example.com", null, false);
            await AddOfferIssueAsync(otherLocationOffer, guest.LocationGuestId);

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "offer-not-redeemed",
                new CampaignAudienceScope(OfferId: otherLocationOffer)
            );

            Assert.Equal(0, result.Matched);
        }

        [Fact]
        public async Task EvaluateAsync_RecentRedeemers_DefaultsToLast30DaysAndLabelsIt()
        {
            var seeded = await SeedLocationAsync();
            var offerId = await AddOfferAsync(seeded.LocationId);
            var recent = await AddGuestAsync(seeded, "Recent", "recent@example.com", null, false);
            var old = await AddGuestAsync(seeded, "Old", "old@example.com", null, false);
            await AddOfferIssueAsync(offerId, recent.LocationGuestId, redeemedAtUtc: DateTime.UtcNow.AddDays(-10));
            await AddOfferIssueAsync(offerId, old.LocationGuestId, redeemedAtUtc: DateTime.UtcNow.AddDays(-45));

            var result = await _eligibility.EvaluateAsync(seeded.LocationId, "recent-redeemers");

            Assert.True(result.Evaluable);
            Assert.Equal(1, result.Matched);
            Assert.Equal(CampaignEligibilityService.DefaultRecentRedeemerLookbackDays, result.LookbackDays);
            Assert.Equal(CampaignEligibilityService.DefaultRecentRedeemerTimeZone, result.LookbackTimeZone);
            Assert.Equal(30, result.LookbackDays);
        }

        [Fact]
        public async Task EvaluateAsync_RecentRedeemers_UsesStoredPeriodWithoutSubstitution()
        {
            var seeded = await SeedLocationAsync();
            var offerId = await AddOfferAsync(seeded.LocationId);
            var threeDays = await AddGuestAsync(seeded, "Three", "three@example.com", null, false);
            var tenDays = await AddGuestAsync(seeded, "Ten", "ten@example.com", null, false);
            await AddOfferIssueAsync(offerId, threeDays.LocationGuestId, redeemedAtUtc: DateTime.UtcNow.AddDays(-3));
            await AddOfferIssueAsync(offerId, tenDays.LocationGuestId, redeemedAtUtc: DateTime.UtcNow.AddDays(-10));

            var result = await _eligibility.EvaluateAsync(
                seeded.LocationId,
                "recent-redeemers",
                new CampaignAudienceScope(RecentRedeemerLookbackDays: 7)
            );

            Assert.Equal(1, result.Matched);
            Assert.Equal(7, result.LookbackDays);
        }

        [Fact]
        public async Task EvaluateAsync_RecentRedeemers_ExcludesOtherLocationVoidedAndUnredeemed()
        {
            var seeded = await SeedLocationAsync();
            var other = await SeedLocationAsync();
            var offerId = await AddOfferAsync(seeded.LocationId);
            var otherOfferId = await AddOfferAsync(other.LocationId);

            var voided = await AddGuestAsync(seeded, "Voided", "v@example.com", null, false);
            var unredeemed = await AddGuestAsync(seeded, "Claimed Only", "u@example.com", null, false);
            var elsewhere = await AddGuestAsync(other, "Elsewhere", "e@example.com", null, false);
            await AddOfferIssueAsync(offerId, voided.LocationGuestId,
                redeemedAtUtc: DateTime.UtcNow.AddDays(-2), voidedAtUtc: DateTime.UtcNow.AddDays(-1));
            await AddOfferIssueAsync(offerId, unredeemed.LocationGuestId);
            await AddOfferIssueAsync(otherOfferId, elsewhere.LocationGuestId,
                redeemedAtUtc: DateTime.UtcNow.AddDays(-2));

            var result = await _eligibility.EvaluateAsync(seeded.LocationId, "recent-redeemers");

            Assert.Equal(0, result.Matched);
        }

        [Fact]
        public async Task EvaluateAsync_RecentRedeemers_NoConsentIsMatchedButNotEligible()
        {
            var seeded = await SeedLocationAsync();
            var offerId = await AddOfferAsync(seeded.LocationId);
            var guest = await AddGuestAsync(seeded, "Opted Out", "out@example.com", null, offersOptOut: true);
            await AddOfferIssueAsync(offerId, guest.LocationGuestId, redeemedAtUtc: DateTime.UtcNow.AddDays(-1));

            var result = await _eligibility.EvaluateAsync(seeded.LocationId, "recent-redeemers");

            Assert.Equal(1, result.Matched);
            Assert.Equal(0, result.CurrentlyEligible);
        }

        [Fact]
        public void RecentRedeemerWindowStart_AnchorsToLocalCalendarDay()
        {
            // 23:30 UTC on 10 Jul 2026 is 00:30 on 11 Jul in London (BST).
            var nowUtc = new DateTime(2026, 7, 10, 23, 30, 0, DateTimeKind.Utc);

            var fromUtc = CampaignEligibilityService.RecentRedeemerWindowStartUtc(2, "Europe/London", nowUtc);

            // Two local days: 11 Jul and 10 Jul. Start is local midnight of 10 Jul.
            Assert.Equal(new DateTime(2026, 7, 9, 23, 0, 0, DateTimeKind.Utc), fromUtc);
        }

        private async Task<int> AddOfferAsync(int locationId, string title = "Free dessert")
        {
            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Title = title,
                Description = "Test offer",
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();
            return offer.Id;
        }

        private async Task AddOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            DateTime? redeemedAtUtc = null,
            DateTime? voidedAtUtc = null,
            DateTime? cancelledAtUtc = null,
            DateTime? expiryAtUtc = null
        )
        {
            _context.OfferIssues.Add(new OfferIssue
            {
                CatalogOfferId = catalogOfferId,
                LocationGuestId = locationGuestId,
                ClaimCode = $"TUM-{Guid.NewGuid():N}"[..12],
                IssuedAtUtc = DateTime.UtcNow.AddDays(-14),
                ClaimedAtUtc = DateTime.UtcNow.AddDays(-14),
                RedeemedAtUtc = redeemedAtUtc,
                RedemptionVoidedAtUtc = voidedAtUtc,
                CancelledAtUtc = cancelledAtUtc,
                ExpiryAtUtc = expiryAtUtc ?? DateTime.UtcNow.AddDays(30),
                Source = OfferIssueSources.Campaign,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Free dessert",
                Description = "Test offer",
                Validity = CatalogOfferValidity.Days30AfterIssue,
            });
            await _context.SaveChangesAsync();
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
                MarketingPreference = LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(offersOptOut),

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

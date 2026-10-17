using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace ProductDemoSeed;

internal static class EngagementSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        int locationId,
        int ownerUserId,
        Dictionary<string, LocationGuest> guestsByName,
        List<Feedback> feedback
    )
    {
        var offers = await SeedOffersAsync(db, locationId);
        await SeedRecoveriesAsync(db, locationId, ownerUserId, guestsByName, feedback, offers);
        await SeedCampaignsAndIssuesAsync(db, locationId, guestsByName, offers);
    }

    private static async Task<Dictionary<string, CatalogOffer>> SeedOffersAsync(
        ApplicationDbContext db,
        int locationId
    )
    {
        var defs = new (string Title, CatalogOfferType Type, string Status, decimal? Pct, decimal? Amt, string? FreeItem)[]
        {
            ("14% off your next order", CatalogOfferType.PercentageDiscount, CatalogOfferStatus.Active, 14m, null, null),
            ("Free house brownie", CatalogOfferType.FreeItem, CatalogOfferStatus.Active, null, null, "House brownie"),
            ("£5 off when you spend £30", CatalogOfferType.FixedDiscount, CatalogOfferStatus.Active, null, 5m, null),
            ("Recovery dessert", CatalogOfferType.FreeItem, CatalogOfferStatus.Active, null, null, "Dessert"),
            ("Bring-a-friend £10 off", CatalogOfferType.FixedDiscount, CatalogOfferStatus.Active, null, 10m, null),
            ("Summer spritz (expired)", CatalogOfferType.FixedDiscount, CatalogOfferStatus.Archived, null, 3m, null),
            ("Paused weekday coffee", CatalogOfferType.FreeItem, CatalogOfferStatus.Paused, null, null, "Filter coffee"),
        };

        var map = new Dictionary<string, CatalogOffer>(StringComparer.OrdinalIgnoreCase);
        var now = DemoSeedConstants.SeedNowUtc;

        foreach (var d in defs)
        {
            var existing = await db.CatalogOffers.FirstOrDefaultAsync(o =>
                o.RestaurantLocationId == locationId && o.Title == d.Title
            );
            if (existing is null)
            {
                existing = new CatalogOffer
                {
                    RestaurantLocationId = locationId,
                    Status = d.Status,
                    OfferType = d.Type,
                    Title = d.Title,
                    Description = $"{d.Title} — Mavero Kitchen demo offer.",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = d.Pct,
                    DiscountAmount = d.Amt,
                    FreeItemText = d.FreeItem,
                    MinimumSpend = d.Title.Contains('£') && d.Title.Contains("30") ? 30m : null,
                    PurchaseRequirement = d.Title.Contains("30")
                        ? CatalogOfferPurchaseRequirement.WithMinimumSpend
                        : CatalogOfferPurchaseRequirement.NoPurchaseRequired,
                    CreatedAt = now.AddDays(-45),
                    UpdatedAt = now.AddDays(-2),
                    CreatedByDisplayName = DemoSeedConstants.OwnerFullName,
                };
                db.CatalogOffers.Add(existing);
            }
            else
            {
                existing.Status = d.Status;
                existing.OfferType = d.Type;
                existing.DiscountPercentage = d.Pct;
                existing.DiscountAmount = d.Amt;
                existing.FreeItemText = d.FreeItem;
            }

            map[d.Title] = existing;
        }

        await db.SaveChangesAsync();
        return map;
    }

    private static async Task SeedRecoveriesAsync(
        ApplicationDbContext db,
        int locationId,
        int ownerUserId,
        Dictionary<string, LocationGuest> guestsByName,
        List<Feedback> feedback,
        Dictionary<string, CatalogOffer> offers
    )
    {
        Feedback? Find(string guestName, string commentContains) =>
            feedback.FirstOrDefault(f =>
                f.GuestName.Equals(guestName, StringComparison.OrdinalIgnoreCase)
                && f.Comment.Contains(commentContains, StringComparison.OrdinalIgnoreCase)
            );

        var plans = new (string Guest, string Needle, string Body, bool WithResponse)[]
        {
            ("Grace Turner", "chicken was dry", "We're sorry about the dry chicken and slow follow-up. We'd like to make this right.", true),
            ("Aisha Khan", "lukewarm", "Thanks for telling us about the lukewarm main — draft recovery prepared with a brownie offer.", true),
            ("Chloe Evans", "dessert order was forgotten", "Apologies for the forgotten dessert. We've removed it and added a recovery treat.", true),
            ("Owen Blake", "extra side", "Sorry about the incorrect bill item — glad we could correct it straight away.", true),
            ("Adam Cole", "35 minutes", "Sorry for the long gap between courses. Here's £5 off your next visit over £30.", true),
            ("Sofia Reed", "missing one portion", "We're reviewing the missing fries from takeaway and will follow up.", false),
            ("Fatima Noor", "allergy information", "We've reviewed menu allergy wording internally after your note.", true),
        };

        foreach (var plan in plans)
        {
            var fb = Find(plan.Guest, plan.Needle);
            if (fb is null)
            {
                continue;
            }

            var exists = await db.FeedbackGuestResponses.AnyAsync(r => r.FeedbackId == fb.Id);
            if (exists || !plan.WithResponse)
            {
                continue;
            }

            db.FeedbackGuestResponses.Add(
                new FeedbackGuestResponse
                {
                    FeedbackId = fb.Id,
                    Channel = FeedbackGuestResponseChannel.Email,
                    Intent = FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                    MaskedDestination = Mask(fb.GuestContact),
                    Subject = "Thanks for your feedback — Mavero Kitchen",
                    Body = plan.Body,
                    Purpose = "recovery",
                    Tone = "warm",
                    AuthorUserId = ownerUserId,
                    AuthorDisplayName = DemoSeedConstants.OwnerFullName,
                    CreatedAt = fb.CreatedAt.AddHours(6),
                    EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.NotApplicable,
                }
            );

            // Attach recovery offer issues for key recoveries.
            if (guestsByName.TryGetValue(plan.Guest, out var guest))
            {
                CatalogOffer? offer = plan.Guest switch
                {
                    "Grace Turner" => offers.GetValueOrDefault("14% off your next order"),
                    "Aisha Khan" => offers.GetValueOrDefault("Free house brownie"),
                    "Chloe Evans" => offers.GetValueOrDefault("Recovery dessert"),
                    "Adam Cole" => offers.GetValueOrDefault("£5 off when you spend £30"),
                    _ => null,
                };
                if (offer != null)
                {
                    await AddIssueAsync(
                        db,
                        offer,
                        guest.Id,
                        fb.Id,
                        OfferIssueSources.Recovery,
                        campaignId: null,
                        claimed: true,
                        redeemed: plan.Guest is "Chloe Evans" or "Adam Cole",
                        issuedAt: fb.CreatedAt.AddHours(7)
                    );
                }
            }
        }

        await db.SaveChangesAsync();

        var locationFeedbackIds = await db.Feedbacks
            .Where(f => f.RestaurantLocationId == locationId)
            .Select(f => f.Id)
            .ToListAsync();

        var started = await db.FeedbackGuestResponses.CountAsync(r =>
            locationFeedbackIds.Contains(r.FeedbackId)
            && r.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
        );

        var recentNegative = await db.Feedbacks
            .Where(f =>
                locationFeedbackIds.Contains(f.Id)
                && f.Sentiment == FeedbackSentiment.Negative
                && f.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
            )
            .OrderByDescending(f => f.CreatedAt)
            .Take(50)
            .ToListAsync();

        // Pad until target across Negative then Neutral (Last-30).
        var padPool = await db.Feedbacks
            .Where(f =>
                locationFeedbackIds.Contains(f.Id)
                && f.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
                && (
                    f.Sentiment == FeedbackSentiment.Negative
                    || f.Sentiment == FeedbackSentiment.Neutral
                )
            )
            .OrderBy(f => f.Sentiment == FeedbackSentiment.Negative ? 0 : 1)
            .ThenByDescending(f => f.CreatedAt)
            .Take(120)
            .ToListAsync();

        foreach (var fb in padPool)
        {
            if (started >= DemoSeedConstants.TargetRecoveriesStarted)
            {
                break;
            }

            if (await db.FeedbackGuestResponses.AnyAsync(r => r.FeedbackId == fb.Id))
            {
                continue;
            }

            db.FeedbackGuestResponses.Add(
                new FeedbackGuestResponse
                {
                    FeedbackId = fb.Id,
                    Channel = FeedbackGuestResponseChannel.Email,
                    Intent = FeedbackRecoveryIntent.RespondToGuest,
                    MaskedDestination = Mask(fb.GuestContact),
                    Subject = "Following up on your visit",
                    Body = "Thank you for sharing your experience. We're looking into this and appreciate your patience.",
                    AuthorUserId = ownerUserId,
                    AuthorDisplayName = DemoSeedConstants.OwnerFullName,
                    CreatedAt = fb.CreatedAt.AddHours(8),
                    EmailDeliveryStatus = GuestResponseEmailDeliveryStatus.NotApplicable,
                }
            );
            started++;
        }

        await db.SaveChangesAsync();
        await SeedRecoveryCompletionsAsync(db, ownerUserId, locationFeedbackIds);
        await RealignNeedsAttentionAsync(db, locationId);
    }

    private static async Task SeedRecoveryCompletionsAsync(
        ApplicationDbContext db,
        int ownerUserId,
        List<int> locationFeedbackIds
    )
    {
        var completed = await db.FeedbackRecoveryCompletions.CountAsync(c =>
            locationFeedbackIds.Contains(c.FeedbackId)
            && c.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
        );

        // Prefer non-NeedsAttention rows so completing recoveries does not
        // collapse the §5 Needs attention = 44 KPI.
        var candidates = await db.Feedbacks
            .Where(f =>
                locationFeedbackIds.Contains(f.Id)
                && f.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
                && db.FeedbackGuestResponses.Any(r => r.FeedbackId == f.Id)
            )
            .OrderBy(f =>
                f.Sentiment == FeedbackSentiment.Negative
                && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
                    ? 1
                    : 0
            )
            .ThenByDescending(f => f.CreatedAt)
            .Take(50)
            .ToListAsync();

        foreach (var fb in candidates)
        {
            if (completed >= DemoSeedConstants.TargetRecoveriesCompleted)
            {
                break;
            }

            if (await db.FeedbackRecoveryCompletions.AnyAsync(c => c.FeedbackId == fb.Id))
            {
                completed++;
                continue;
            }

            var from = fb.WorkflowStatus;
            if (from == FeedbackWorkflowStatus.Resolved)
            {
                from = FeedbackWorkflowStatus.InProgress;
            }

            var change = new FeedbackWorkflowStatusChange
            {
                FeedbackId = fb.Id,
                FromStatus = from,
                ToStatus = FeedbackWorkflowStatus.Resolved,
                AuthorUserId = ownerUserId,
                AuthorDisplayName = DemoSeedConstants.OwnerFullName,
                CreatedAt = fb.CreatedAt.AddHours(20),
            };
            db.FeedbackWorkflowStatusChanges.Add(change);
            await db.SaveChangesAsync();

            fb.WorkflowStatus = FeedbackWorkflowStatus.Resolved;
            db.FeedbackRecoveryCompletions.Add(
                new FeedbackRecoveryCompletion
                {
                    FeedbackId = fb.Id,
                    Intent = FeedbackRecoveryIntent.RespondToGuest,
                    WorkflowStatusChangeId = change.Id,
                    AuthorUserId = ownerUserId,
                    AuthorDisplayName = DemoSeedConstants.OwnerFullName,
                    CreatedAt = change.CreatedAt,
                }
            );
            completed++;
        }

        await db.SaveChangesAsync();
    }

    private static async Task RealignNeedsAttentionAsync(
        ApplicationDbContext db,
        int locationId
    )
    {
        var cutoff = DemoSeedConstants.SeedNowUtc.AddDays(-30);
        var rows = await db.Feedbacks
            .Where(f =>
                f.RestaurantLocationId == locationId
                && f.CreatedAt >= cutoff
                && f.ClassificationStatus == ClassificationStatus.Succeeded
                && f.Sentiment == FeedbackSentiment.Negative
            )
            .ToListAsync();

        var completedIds = (
            await db.FeedbackRecoveryCompletions
                .Where(c => rows.Select(r => r.Id).Contains(c.FeedbackId))
                .Select(c => c.FeedbackId)
                .ToListAsync()
        ).ToHashSet();

        bool IsNeeds(Feedback f) =>
            f.WorkflowStatus != FeedbackWorkflowStatus.Resolved;

        var needs = rows.Where(IsNeeds).ToList();
        var target = DemoSeedConstants.TargetNeedsAttention;

        if (needs.Count < target)
        {
            // Prefer reopening rows without a recovery completion; if still
            // short, reopen Resolved Negatives that already completed so the
            // §5 Needs attention KPI stays at 44 (completion rows remain).
            var reopen = rows
                .Where(f => f.WorkflowStatus == FeedbackWorkflowStatus.Resolved)
                .OrderBy(f => completedIds.Contains(f.Id) ? 1 : 0)
                .ThenByDescending(f => f.CreatedAt)
                .Take(target - needs.Count)
                .ToList();
            foreach (var f in reopen)
            {
                f.WorkflowStatus = FeedbackWorkflowStatus.InProgress;
            }

            needs = rows.Where(IsNeeds).ToList();
        }

        if (needs.Count < target)
        {
            // Promote Neutral → Negative InProgress when the Negative pool is short.
            var promote = await db.Feedbacks
                .Where(f =>
                    f.RestaurantLocationId == locationId
                    && f.CreatedAt >= cutoff
                    && f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.Sentiment == FeedbackSentiment.Neutral
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(target - needs.Count)
                .ToListAsync();
            foreach (var f in promote)
            {
                f.Sentiment = FeedbackSentiment.Negative;
                f.WorkflowStatus = FeedbackWorkflowStatus.InProgress;
            }
        }
        else if (needs.Count > target)
        {
            foreach (var f in needs.Skip(target))
            {
                f.WorkflowStatus = FeedbackWorkflowStatus.Resolved;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCampaignsAndIssuesAsync(
        ApplicationDbContext db,
        int locationId,
        Dictionary<string, LocationGuest> guestsByName,
        Dictionary<string, CatalogOffer> offers
    )
    {
        var now = DemoSeedConstants.SeedNowUtc;
        var campaignDefs = new (
            string Name,
            string Channel,
            string Status,
            int Sent,
            int Opened,
            int Claims,
            int Redemptions,
            string? OfferTitle,
            int DaysAgo
        )[]
        {
            ("Tuesday Table Boost", "email", "sent", 612, 126, 49, 27, "14% off your next order", 20),
            ("We Miss You - September", "email", "sent", 487, 93, 38, 19, "£5 off when you spend £30", 28),
            ("Weekend Brownie Treat", "sms", "sent", 176, 41, 28, 17, "Free house brownie", 14),
            ("Autumn Menu Return", "email", "sent", 731, 143, 52, 28, "Bring-a-friend £10 off", 10),
            ("Monday Lunch Return", "email", "scheduled", 342, 46, 11, 3, "14% off your next order", 2),
        };

        var guestPool = guestsByName.Values
            .Where(g => g.MarketingPreference == LocationGuestMarketingPreference.Allowed)
            .ToList();
        if (guestPool.Count == 0)
        {
            guestPool = guestsByName.Values.ToList();
        }

        var code = 100000;
        foreach (var def in campaignDefs)
        {
            var existing = await db.Campaigns.FirstOrDefaultAsync(c =>
                c.RestaurantLocationId == locationId && c.Name == def.Name
            );
            CatalogOffer? offer = def.OfferTitle is null
                ? null
                : offers.GetValueOrDefault(def.OfferTitle);

            if (existing is null)
            {
                existing = new Campaign
                {
                    RestaurantLocationId = locationId,
                    Status = def.Status,
                    Name = def.Name,
                    GoalId = "boost-quieter-time",
                    Channel = def.Channel,
                    OfferId = offer?.Id,
                    OfferStance = offer is null ? "no-offer" : "existing-offer",
                    MessageSubject = def.Name,
                    MessageBody = $"Demo campaign body for {def.Name}.",
                    ScheduleMode = "send-now",
                    ScheduledAtUtc = now.AddDays(-def.DaysAgo),
                    UpdatedAt = now.AddDays(-def.DaysAgo),
                    CreatedAt = now.AddDays(-def.DaysAgo - 1),
                };
                db.Campaigns.Add(existing);
                await db.SaveChangesAsync();
            }
            else
            {
                existing.Status = def.Status;
                existing.Channel = def.Channel;
                existing.OfferId = offer?.Id;
            }

            var deliveryCount = await db.CampaignRecipientDeliveries.CountAsync(d =>
                d.CampaignId == existing.Id
            );
            if (deliveryCount == 0)
            {
                // Seed enough Accepted/Opened rows so list KPIs approach §11 (capped by guest pool).
                var sample = Math.Min(Math.Max(def.Sent / 3, def.Opened + 40), guestPool.Count);
                sample = Math.Max(sample, Math.Min(def.Opened + 20, guestPool.Count));
                for (var i = 0; i < sample; i++)
                {
                    var g = guestPool[i % guestPool.Count];
                    db.CampaignRecipientDeliveries.Add(
                        new CampaignRecipientDelivery
                        {
                            CampaignId = existing.Id,
                            LocationGuestId = g.Id,
                            Channel = def.Channel,
                            Outcome = CampaignFireService.AcceptedOutcome,
                            AcceptedAtUtc = now.AddDays(-def.DaysAgo).AddMinutes(i),
                            OpenedAtUtc = i < def.Opened
                                ? now.AddDays(-def.DaysAgo).AddHours(2).AddMinutes(i)
                                : null,
                            ProviderMessageId = $"demo-{existing.Id}-{i}",
                            AcceptedUnits = 1,
                        }
                    );
                    db.CampaignFrozenRecipients.Add(
                        new CampaignFrozenRecipient
                        {
                            CampaignId = existing.Id,
                            LocationGuestId = g.Id,
                            FrozenAtUtc = now.AddDays(-def.DaysAgo).AddMinutes(-30),
                            AcceptedAtUtc = now.AddDays(-def.DaysAgo).AddMinutes(i),
                        }
                    );
                }
            }

            if (offer != null)
            {
                var existingIssues = await db.OfferIssues.CountAsync(o =>
                    o.CampaignId == existing.Id
                );
                var needClaims = Math.Max(0, def.Claims - existingIssues);
                for (var i = 0; i < needClaims; i++)
                {
                    var g = guestPool[(i + def.DaysAgo) % guestPool.Count];
                    var redeemed = i < def.Redemptions;
                    await AddIssueAsync(
                        db,
                        offer,
                        g.Id,
                        feedbackId: null,
                        OfferIssueSources.Campaign,
                        existing.Id,
                        claimed: true,
                        redeemed,
                        issuedAt: now.AddDays(-def.DaysAgo).AddHours(i % 20),
                        claimCode: $"TUM-{code++:D6}"
                    );
                }
            }
        }

        // §10 per-title claim / redemption targets (Last-30 window).
        var perTitle = new (string Title, int Claims, int Redemptions)[]
        {
            ("14% off your next order", 78, 35),
            ("Free house brownie", 43, 24),
            ("£5 off when you spend £30", 31, 19),
            ("Recovery dessert", 12, 8),
            ("Bring-a-friend £10 off", 14, 8),
        };

        var padIndex = 0;
        foreach (var (title, claimTarget, redeemTarget) in perTitle)
        {
            if (!offers.TryGetValue(title, out var offer))
            {
                continue;
            }

            var currentClaims = await db.OfferIssues.CountAsync(o =>
                o.CatalogOfferId == offer.Id && o.ClaimedAtUtc != null
            );
            var currentRedeems = await db.OfferIssues.CountAsync(o =>
                o.CatalogOfferId == offer.Id
                && o.RedeemedAtUtc != null
                && o.RedemptionVoidedAtUtc == null
            );

            for (; currentClaims < claimTarget; currentClaims++, padIndex++)
            {
                var g = guestPool[padIndex % guestPool.Count];
                var redeemed = currentRedeems < redeemTarget;
                if (redeemed)
                {
                    currentRedeems++;
                }

                await AddIssueAsync(
                    db,
                    offer,
                    g.Id,
                    null,
                    OfferIssueSources.GuestFormThankYou,
                    null,
                    claimed: true,
                    redeemed,
                    issuedAt: now.AddDays(-(padIndex % 28)).AddHours(padIndex % 10),
                    claimCode: $"TUM-{code++:D6}"
                );
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task AddIssueAsync(
        ApplicationDbContext db,
        CatalogOffer offer,
        int locationGuestId,
        int? feedbackId,
        string source,
        int? campaignId,
        bool claimed,
        bool redeemed,
        DateTime issuedAt,
        string? claimCode = null
    )
    {
        var code = claimCode ?? $"TUM-{Random.Shared.Next(100000, 999999)}";
        if (await db.OfferIssues.AnyAsync(o => o.ClaimCode == code))
        {
            code = $"TUM-{Random.Shared.Next(100000, 999999)}";
        }

        db.OfferIssues.Add(
            new OfferIssue
            {
                CatalogOfferId = offer.Id,
                LocationGuestId = locationGuestId,
                ClaimCode = code,
                IssuedAtUtc = issuedAt,
                ClaimedAtUtc = claimed ? issuedAt : null,
                RedeemedAtUtc = redeemed ? issuedAt.AddDays(3) : null,
                Source = source,
                CampaignId = campaignId,
                FeedbackId = feedbackId,
                ExpiryAtUtc = issuedAt.AddDays(14),
                OfferType = offer.OfferType,
                Title = offer.Title,
                Description = offer.Description,
                Validity = offer.Validity,
                DiscountPercentage = offer.DiscountPercentage,
                DiscountAmount = offer.DiscountAmount,
                FreeItemText = offer.FreeItemText,
                PurchaseRequirement = offer.PurchaseRequirement,
                MinimumSpend = offer.MinimumSpend,
            }
        );
        await Task.CompletedTask;
    }

    private static string Mask(string contact)
    {
        if (string.IsNullOrWhiteSpace(contact))
        {
            return "***";
        }

        if (contact.Contains('@'))
        {
            var parts = contact.Split('@');
            return $"{parts[0][..Math.Min(2, parts[0].Length)]}***@{parts[1]}";
        }

        return $"***{contact[^3..]}";
    }
}

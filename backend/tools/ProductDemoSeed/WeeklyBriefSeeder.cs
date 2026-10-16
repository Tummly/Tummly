using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace ProductDemoSeed;

internal static class WeeklyBriefSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        int locationId,
        int ownerUserId,
        string locationName
    )
    {
        // Four weeks ending with the §12.2 fixture (28 Sep – 04 Oct 2026).
        var weeks = new (DateTime Start, WeeklyBriefMetrics Metrics, string Headline)[]
        {
            (
                DemoSeedConstants.AnchorWeekStartUtc.AddDays(-21),
                new WeeklyBriefMetrics(55, 390, 88, 58, 16, 14, 9, TagCounts(22, 30, 14), 5, 36, 18, 1, 480, 2),
                "Solid midweek covers with steady service praise."
            ),
            (
                DemoSeedConstants.AnchorWeekStartUtc.AddDays(-14),
                new WeeklyBriefMetrics(61, 405, 94, 60, 18, 16, 10, TagCounts(25, 33, 15), 5, 39, 20, 2, 720, 3),
                "Offer claims climbed; keep the brownie attach warm."
            ),
            (
                DemoSeedConstants.AnchorWeekStartUtc.AddDays(-7),
                new WeeklyBriefMetrics(68, 420, 99, 63, 17, 19, 12, TagCounts(27, 35, 18), 5, 42, 21, 2, 850, 2),
                "Wait spikes returned on Saturday dinner — watch the pass."
            ),
            (
                DemoSeedConstants.AnchorWeekStartUtc,
                // §12.2 canonical week
                new WeeklyBriefMetrics(
                    GuestsJoined: 72,
                    QrScanEvents: 438,
                    FeedbackCount: 102,
                    PositiveFeedbackCount: 68,
                    NeutralFeedbackCount: 17,
                    NegativeFeedbackCount: 17,
                    NeedsAttentionCount: 11,
                    DetectedTagCounts: new Dictionary<string, int>
                    {
                        ["Staff friendliness"] = 29,
                        ["Service"] = 24,
                        ["Wait time"] = 12,
                        ["Food quality"] = 22,
                        ["Billing"] = 8,
                    },
                    ActiveOffers: 5,
                    ClaimsInWeek: 45,
                    RedemptionsInWeek: 24,
                    CampaignsSentInWeek: 2,
                    CampaignRecipientsReached: 1099,
                    UnsubscribesInWeek: 3
                ),
                "Busy covers, stronger staff praise, and evening bill delays to own."
            ),
        };

        foreach (var (start, metrics, headline) in weeks)
        {
            var weekKey = $"monday:{start:yyyy-MM-dd}";
            var body = new WeeklyBriefBody(
                Headline: headline,
                Capture: new WeeklyBriefSection(
                    true,
                    "QR scans and new guests stayed healthy across dining and counter placements.",
                    new Dictionary<string, int>
                    {
                        ["scans"] = metrics.QrScanEvents,
                        ["guestsJoined"] = metrics.GuestsJoined,
                    }
                ),
                Feedback: new WeeklyBriefSection(
                    true,
                    start == DemoSeedConstants.AnchorWeekStartUtc
                        ? "Staff friendliness led positives. Wait/payment timing rose versus the prior week — assign an owner for evening follow-up."
                        : "Service and food quality led the week; keep Needs attention moving.",
                    new Dictionary<string, int>
                    {
                        ["feedback"] = metrics.FeedbackCount,
                        ["positive"] = metrics.PositiveFeedbackCount,
                        ["needsAttention"] = metrics.NeedsAttentionCount,
                    }
                ),
                Offers: new WeeklyBriefSection(
                    true,
                    "Catalog offers kept converting with healthy claim and redemption follow-through.",
                    new Dictionary<string, int>
                    {
                        ["claims"] = metrics.ClaimsInWeek,
                        ["redemptions"] = metrics.RedemptionsInWeek,
                    }
                ),
                Campaigns: new WeeklyBriefSection(
                    true,
                    "Campaign reach stayed useful for quieter dayparts.",
                    new Dictionary<string, int>
                    {
                        ["campaignsSent"] = metrics.CampaignsSentInWeek,
                        ["recipients"] = metrics.CampaignRecipientsReached,
                    }
                ),
                WatchNext: start == DemoSeedConstants.AnchorWeekStartUtc
                    ?
                    [
                        "Review evening bill/payment delays and assign an owner for service follow-up.",
                        "Use a quiet-day return Campaign for eligible guests rather than a blanket send.",
                        "Keep Tuesday Table Boost attached while redemptions stay healthy.",
                    ]
                    :
                    [
                        "Keep Needs attention Feedback moving toward Resolved.",
                        "Protect peak lunch scan placements.",
                    ]
            );

            var generatedAt = start.AddDays(7).AddHours(8);
            var existing = await db.WeeklyBriefs.FirstOrDefaultAsync(w =>
                w.LocationId == locationId && w.WeekKey == weekKey
            );

            if (existing is null)
            {
                db.WeeklyBriefs.Add(
                    new WeeklyBrief
                    {
                        LocationId = locationId,
                        WeekKey = weekKey,
                        Status = WeeklyBriefStatus.Succeeded,
                        GeneratedAtUtc = generatedAt,
                        BodyJson = JsonSerializer.Serialize(body, WeeklyBriefStoreJson.Options),
                        MetricsJson = JsonSerializer.Serialize(metrics, WeeklyBriefStoreJson.Options),
                    }
                );
            }
            else
            {
                existing.Status = WeeklyBriefStatus.Succeeded;
                existing.GeneratedAtUtc = generatedAt;
                existing.BodyJson = JsonSerializer.Serialize(body, WeeklyBriefStoreJson.Options);
                existing.MetricsJson = JsonSerializer.Serialize(metrics, WeeklyBriefStoreJson.Options);
                existing.ErrorInfo = null;
            }

            if (start == DemoSeedConstants.AnchorWeekStartUtc)
            {
                var dedupe = WeeklyBriefReadyNotifier.DedupeKeyFor(locationId, weekKey);
                var notice = await db.Notifications.FirstOrDefaultAsync(n =>
                    n.UserId == ownerUserId
                    && n.Type == WeeklyBriefReadyNotifier.NotificationType
                    && n.DedupeKey == dedupe
                );

                if (notice is null)
                {
                    db.Notifications.Add(
                        new Notification
                        {
                            UserId = ownerUserId,
                            Category = "weekly-brief-reminders",
                            Type = WeeklyBriefReadyNotifier.NotificationType,
                            Title = $"Weekly brief ready — {locationName}",
                            Body = $"Your weekly summary for {locationName} is ready.",
                            CtaLabel = WeeklyBriefReadyNotifier.CtaLabel,
                            CtaHref = WeeklyBriefReadyNotifier.ReportsWeeklyBriefCtaHref(
                                "Single",
                                locationId
                            ),
                            DedupeKey = dedupe,
                            CreatedAt = generatedAt,
                        }
                    );
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static Dictionary<string, int> TagCounts(int food, int service, int wait) =>
        new()
        {
            ["Food quality"] = food,
            ["Service"] = service,
            ["Wait time"] = wait,
            ["Atmosphere"] = 10,
            ["Value"] = 12,
        };
}

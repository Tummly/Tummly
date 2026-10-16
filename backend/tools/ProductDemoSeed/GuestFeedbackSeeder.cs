using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace ProductDemoSeed;

internal static class GuestFeedbackSeeder
{
    private sealed record AnchorGuest(
        string Name,
        string? Email,
        string? Mobile,
        LocationGuestMarketingPreference Marketing,
        bool Anonymous
    );

    private sealed record AnchorFeedback(
        string ExternalKey,
        DateTime SubmittedUtc,
        string PlacementKey,
        string GuestName,
        string Comment,
        FeedbackSentiment Sentiment,
        DetectedTag[] Tags,
        FeedbackWorkflowStatus Status
    );

    private static readonly AnchorGuest[] AnchorGuests =
    [
        new("Maya Collins", "maya.collins@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Daniel Brooks", null, "07700900421", LocationGuestMarketingPreference.Allowed, false),
        new("Aisha Khan", "aisha.khan@example.test", null, LocationGuestMarketingPreference.NotRecorded, false),
        new("Leo Martin", "leo.martin@example.test", null, LocationGuestMarketingPreference.OptedOut, false),
        new("Priya Shah", "priya.shah@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Noah Wright", null, "07700900422", LocationGuestMarketingPreference.OptedOut, false),
        new("Grace Turner", "grace.turner@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Chloe Evans", "chloe.evans@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Sofia Reed", "sofia.reed@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Owen Blake", "owen.blake@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Adam Cole", "adam.cole@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Fatima Noor", "fatima.noor@example.test", null, LocationGuestMarketingPreference.Allowed, false),
        new("Walk-in 1141", null, null, LocationGuestMarketingPreference.NotRecorded, true),
        new("Walk-in 1128", null, null, LocationGuestMarketingPreference.NotRecorded, true),
        new("Walk-in 1107", null, null, LocationGuestMarketingPreference.NotRecorded, true),
        new("Walk-in 1088", null, null, LocationGuestMarketingPreference.NotRecorded, true),
        new("Walk-in 1043", null, null, LocationGuestMarketingPreference.NotRecorded, true),
        new("Walk-in 1002", null, null, LocationGuestMarketingPreference.NotRecorded, true),
        new("Walk-in 921", null, null, LocationGuestMarketingPreference.NotRecorded, true),
    ];

    private static readonly AnchorFeedback[] Anchors =
    [
        new("DEMO-FB-1042", At(10, 5, 12, 27), "table", "Walk-in 1141", "Food was excellent and the server was lovely, but it took almost 20 minutes to get the bill after we asked.", FeedbackSentiment.Neutral, [DetectedTag.Billing, DetectedTag.WaitTime], FeedbackWorkflowStatus.New),
        new("DEMO-FB-1041", At(10, 5, 10, 44), "smart", "Maya Collins", "Really good brunch. The team were friendly and our coffees arrived quickly.", FeedbackSentiment.Positive, [DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1039", At(10, 4, 20, 51), "table", "Daniel Brooks", "The steak was cooked exactly as ordered and the server checked back at the right time.", FeedbackSentiment.Positive, [DetectedTag.FoodQuality, DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1038", At(10, 4, 19, 38), "table", "Walk-in 1128", "The food was good but the music was loud enough that we struggled to hear each other.", FeedbackSentiment.Neutral, [DetectedTag.Atmosphere], FeedbackWorkflowStatus.New),
        new("DEMO-FB-1036", At(10, 4, 18, 12), "offer", "Aisha Khan", "One of the vegetarian mains arrived lukewarm. It was replaced quickly, which we appreciated.", FeedbackSentiment.Negative, [DetectedTag.FoodQuality], FeedbackWorkflowStatus.InProgress),
        new("DEMO-FB-1034", At(10, 3, 21, 17), "table", "Leo Martin", "We mentioned a nut allergy and the server checked with the kitchen before taking the order. That was reassuring.", FeedbackSentiment.Positive, [DetectedTag.AllergiesDietary, DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1033", At(10, 3, 20, 2), "window", "Walk-in 1107", "We waited at the door for around 12 minutes even though there were free tables inside.", FeedbackSentiment.Negative, [DetectedTag.WaitTime, DetectedTag.BookingSeating], FeedbackWorkflowStatus.InProgress),
        new("DEMO-FB-1031", At(10, 3, 13, 11), "smart", "Priya Shah", "The lunch set menu felt like good value and service was efficient.", FeedbackSentiment.Positive, [DetectedTag.Value, DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1029", At(10, 2, 19, 43), "offer", "Noah Wright", "Loved the brownie and the staff recommendation was spot on.", FeedbackSentiment.Positive, [DetectedTag.FoodQuality, DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1027", At(10, 2, 17, 5), "smart", "Sofia Reed", "Our takeaway bag was missing one portion of fries. Everything else was correct.", FeedbackSentiment.Negative, [DetectedTag.FoodQuality], FeedbackWorkflowStatus.New),
        new("DEMO-FB-1026", At(10, 2, 12, 26), "table", "Ethan Cole", "Quick lunch, friendly welcome and the food came out hot.", FeedbackSentiment.Positive, [DetectedTag.Service, DetectedTag.FoodQuality], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1023", At(10, 1, 21, 48), "window", "Walk-in 1088", "Tables felt quite close together near the window, although the atmosphere was otherwise nice.", FeedbackSentiment.Neutral, [DetectedTag.BookingSeating, DetectedTag.Atmosphere], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1021", At(10, 1, 19, 5), "table", "Grace Turner", "The chicken was dry and we had to wait a while before anyone came back to the table.", FeedbackSentiment.Negative, [DetectedTag.FoodQuality, DetectedTag.Service], FeedbackWorkflowStatus.InProgress),
        new("DEMO-FB-1020", At(10, 1, 13, 42), "offer", "Amelia Hart", "Our server noticed it was a birthday and brought a candle with dessert. Lovely touch.", FeedbackSentiment.Positive, [DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1018", At(9, 30, 20, 17), "table", "Jack Reid", "We had to ask twice for tap water, but the mains were excellent.", FeedbackSentiment.Neutral, [DetectedTag.Service, DetectedTag.FoodQuality], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1016", At(9, 30, 18, 28), "smart", "Nadia Ali", "Great vegan options and everything was clearly explained.", FeedbackSentiment.Positive, [DetectedTag.AllergiesDietary, DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1014", At(9, 29, 21, 3), "table", "Walk-in 1043", "Burger was great but the fries arrived several minutes after the rest of the table had started eating.", FeedbackSentiment.Neutral, [DetectedTag.FoodQuality, DetectedTag.WaitTime], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1012", At(9, 29, 15, 16), "window", "Henry Moss", "Coffee was excellent and the afternoon team were very welcoming.", FeedbackSentiment.Positive, [DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1009", At(9, 28, 20, 40), "table", "Chloe Evans", "Our dessert order was forgotten. The manager apologised and removed it from the bill.", FeedbackSentiment.Negative, [DetectedTag.FoodQuality, DetectedTag.Billing], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1007", At(9, 28, 14, 7), "offer", "Sam Ortiz", "Staff were brilliant helping us find space for a pram and made the visit easy.", FeedbackSentiment.Positive, [DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1004", At(9, 27, 19, 54), "table", "Walk-in 1002", "The specials were explained very quickly and we could not hear all of them.", FeedbackSentiment.Neutral, [DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-1001", At(9, 26, 20, 33), "table", "Ella Quinn", "Really smooth service from start to finish and the pasta was excellent.", FeedbackSentiment.Positive, [DetectedTag.Service, DetectedTag.FoodQuality], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0998", At(9, 25, 18, 14), "smart", "Owen Blake", "The bill included an extra side we did not order. It was corrected straight away.", FeedbackSentiment.Negative, [DetectedTag.Billing, DetectedTag.FoodQuality], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0994", At(9, 24, 12, 55), "window", "Ruby Lane", "Nice window table, good lunch and no long wait even though it was busy.", FeedbackSentiment.Positive, [DetectedTag.Atmosphere, DetectedTag.WaitTime], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0990", At(9, 23, 21, 12), "table", "Adam Cole", "We waited about 35 minutes between starter and main course. The team were polite but it was too long.", FeedbackSentiment.Negative, [DetectedTag.WaitTime, DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0986", At(9, 22, 16, 48), "offer", "Fatima Noor", "Good value overall, but the allergy information on the printed menu could be clearer.", FeedbackSentiment.Neutral, [DetectedTag.AllergiesDietary, DetectedTag.Value], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0981", At(9, 21, 19, 30), "table", "George Hill", "Friendly welcome and the fish tasted really fresh.", FeedbackSentiment.Positive, [DetectedTag.Service, DetectedTag.FoodQuality], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0977", At(9, 20, 17, 22), "smart", "Imogen Day", "The guest link was easy to use and the team checked on us without interrupting too often.", FeedbackSentiment.Positive, [DetectedTag.Service], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0973", At(9, 19, 20, 11), "table", "Walk-in 921", "Loved the atmosphere and lighting. Would come back for drinks.", FeedbackSentiment.Positive, [DetectedTag.Atmosphere], FeedbackWorkflowStatus.Resolved),
        new("DEMO-FB-0968", At(9, 18, 13, 6), "offer", "Ben Shaw", "Soup was good, although the bread arrived after I had nearly finished it.", FeedbackSentiment.Neutral, [DetectedTag.FoodQuality, DetectedTag.WaitTime], FeedbackWorkflowStatus.Resolved),
    ];

    private static readonly string[] FirstNames =
    [
        "Amelia", "James", "Oliver", "Isla", "Mia", "Lucas", "Ava", "Harry", "Freya", "Arthur",
        "Emily", "George", "Oscar", "Poppy", "Jacob", "Lily", "Thomas", "Evie", "Charlie", "Rosie",
    ];

    private static readonly string[] LastNames =
    [
        "Hart", "Patel", "Nguyen", "Brooks", "Singh", "Walsh", "Khan", "Murphy", "Chen", "Reed",
        "Clarke", "Hughes", "Bennett", "Foster", "Hayes", "Powell", "Russell", "Griffin", "Dunn", "Burns",
    ];

    private static readonly string[] CommentBank =
    [
        "Service felt attentive without being pushy.",
        "Really good value for a weekday lunch.",
        "The main course arrived hot and well seasoned.",
        "We enjoyed the atmosphere on a busy Friday night.",
        "Dessert was excellent and worth sharing.",
        "Slight wait for water, otherwise a smooth visit.",
        "Staff handled our allergy note carefully.",
        "Portions were generous and the plate looked great.",
        "A bit noisy near the kitchen pass, but food quality made up for it.",
        "Coffee was strong and the pastry was fresh.",
        "Took longer than expected to be seated at peak time.",
        "Friendly welcome at the door set a good tone.",
        "The set menu was clear and easy to choose from.",
        "One dish was under-seasoned; everything else was fine.",
        "Would happily recommend for a casual dinner.",
        "Clean toilets and tidy dining room throughout.",
        "Card machine took a few tries before it worked.",
        "Kids were made to feel welcome with high chairs ready.",
        "Side salad was crisp and well dressed.",
        "We appreciated the quick replacement when a glass was chipped.",
    ];

    public static async Task<(Dictionary<string, LocationGuest> GuestsByName, List<Feedback> Feedback)>
        SeedAsync(
            ApplicationDbContext db,
            int restaurantId,
            int locationId,
            CaptureSeeder.PlacementBundle placements
        )
    {
        var guestsByName = await SeedGuestsAsync(db, restaurantId, locationId);
        var feedback = await SeedFeedbackAsync(
            db,
            locationId,
            placements,
            guestsByName
        );
        return (guestsByName, feedback);
    }

    private static async Task<Dictionary<string, LocationGuest>> SeedGuestsAsync(
        ApplicationDbContext db,
        int restaurantId,
        int locationId
    )
    {
        var map = new Dictionary<string, LocationGuest>(StringComparer.OrdinalIgnoreCase);
        var now = DemoSeedConstants.SeedNowUtc;

        var existingCount = await db.LocationGuests.CountAsync(lg =>
            lg.RestaurantLocationId == locationId
        );
        if (existingCount >= DemoSeedConstants.TargetGuests)
        {
            var existing = await db.LocationGuests
                .Include(lg => lg.MasterGuest)
                .Where(lg => lg.RestaurantLocationId == locationId)
                .OrderBy(lg => lg.Id)
                .Take(400)
                .ToListAsync();
            foreach (var lg in existing)
            {
                if (!string.IsNullOrWhiteSpace(lg.Name))
                {
                    map[lg.Name] = lg;
                }
            }

            return map;
        }

        foreach (var g in AnchorGuests)
        {
            LocationGuest lg;
            if (g.Anonymous)
            {
                // Anonymous walk-ins are Feedback-only; skip MasterGuest rows.
                continue;
            }

            var email = g.Email ?? $"{Slug(g.Name)}@{DemoSeedConstants.SeedEmailDomain}";
            var master = await db.MasterGuests.FirstOrDefaultAsync(m =>
                m.RestaurantId == restaurantId
                && m.NormalizedEmail == email.ToLowerInvariant()
            );
            if (master is null)
            {
                master = new MasterGuest
                {
                    RestaurantId = restaurantId,
                    Email = email,
                    NormalizedEmail = email.ToLowerInvariant(),
                    Mobile = g.Mobile,
                    NormalizedPhone = g.Mobile is null
                        ? null
                        : new string(g.Mobile.Where(char.IsDigit).ToArray()),
                    CreatedAt = now.AddDays(-40),
                };
                db.MasterGuests.Add(master);
                await db.SaveChangesAsync();
            }

            lg = await db.LocationGuests
                .Include(x => x.MasterGuest)
                .FirstOrDefaultAsync(x =>
                    x.MasterGuestId == master.Id && x.RestaurantLocationId == locationId
                ) ?? new LocationGuest
            {
                MasterGuestId = master.Id,
                MasterGuest = master,
                RestaurantLocationId = locationId,
                Name = g.Name,
                MarketingPreference = g.Marketing,
                CreatedAt = master.CreatedAt,
            };

            if (lg.Id == 0)
            {
                db.LocationGuests.Add(lg);
                await db.SaveChangesAsync();
            }
            else
            {
                lg.Name = g.Name;
                lg.MarketingPreference = g.Marketing;
                lg.MasterGuest ??= master;
            }

            map[g.Name] = lg;
        }

        // Pad guests to ~302 with marketing-eligible mix.
        existingCount = await db.LocationGuests.CountAsync(lg =>
            lg.RestaurantLocationId == locationId
        );
        var need = Math.Max(0, DemoSeedConstants.TargetGuests - existingCount);
        var marketingNeed = DemoSeedConstants.TargetMarketingEligible
            - await db.LocationGuests.CountAsync(lg =>
                lg.RestaurantLocationId == locationId
                && lg.MarketingPreference == LocationGuestMarketingPreference.Allowed
            );

        for (var i = 0; i < need; i++)
        {
            var first = FirstNames[i % FirstNames.Length];
            var last = LastNames[(i * 3) % LastNames.Length];
            var name = $"{first} {last} {i + 1}";
            var email = $"{first}.{last}.{locationId}.{i + 1}@{DemoSeedConstants.SeedEmailDomain}"
                .ToLowerInvariant();
            var allowMarketing = marketingNeed > 0 || i % 3 != 2;
            if (allowMarketing && marketingNeed > 0)
            {
                marketingNeed--;
            }

            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = email,
                NormalizedEmail = email,
                CreatedAt = now.AddDays(-(i % 85) - 1),
            };
            db.MasterGuests.Add(master);
            var lg = new LocationGuest
            {
                MasterGuest = master,
                RestaurantLocationId = locationId,
                Name = name,
                MarketingPreference = allowMarketing
                    ? LocationGuestMarketingPreference.Allowed
                    : (i % 2 == 0
                        ? LocationGuestMarketingPreference.OptedOut
                        : LocationGuestMarketingPreference.NotRecorded),
                CreatedAt = master.CreatedAt,
            };
            db.LocationGuests.Add(lg);
            map[name] = lg;
        }

        await db.SaveChangesAsync();
        return map;
    }

    private static async Task<List<Feedback>> SeedFeedbackAsync(
        ApplicationDbContext db,
        int locationId,
        CaptureSeeder.PlacementBundle placements,
        Dictionary<string, LocationGuest> guestsByName
    )
    {
        var existingFeedback = await db.Feedbacks
            .Where(f => f.RestaurantLocationId == locationId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
        if (existingFeedback.Count >= DemoSeedConstants.TargetFeedback)
        {
            return existingFeedback;
        }

        // Prefer --replace for a clean re-seed; without wipe, skip insert to avoid dupes.
        if (existingFeedback.Count > 0)
        {
            return existingFeedback;
        }

        var rows = new List<Feedback>();
        var qrByKey = new Dictionary<string, int>
        {
            ["table"] = placements.TableTent.Id,
            ["offer"] = placements.OfferCard.Id,
            ["window"] = placements.WindowSticker.Id,
            ["smart"] = placements.SmartGuestLink.Id,
        };

        foreach (var a in Anchors)
        {
            guestsByName.TryGetValue(a.GuestName, out var guest);
            // Also seed extra named guests referenced in anchors
            if (guest is null && !a.GuestName.StartsWith("Walk-in", StringComparison.Ordinal))
            {
                guest = await EnsureNamedGuestAsync(db, locationId, a.GuestName, guestsByName);
            }

            var contact = guest?.MasterGuest?.Email
                ?? guest?.MasterGuest?.Mobile
                ?? $"walkin.{a.ExternalKey.ToLowerInvariant()}@{DemoSeedConstants.SeedEmailDomain}";

            rows.Add(
                new Feedback
                {
                    RestaurantLocationId = locationId,
                    QrCodeId = qrByKey[a.PlacementKey],
                    LocationGuestId = guest?.Id,
                    GuestName = a.GuestName,
                    GuestContact = contact,
                    ContactType = contact.Contains('@') ? ContactType.Email : ContactType.Phone,
                    Comment = a.Comment,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = a.Sentiment,
                    ClassifiedAt = a.SubmittedUtc.AddMinutes(4),
                    DetectedTagsJson = FeedbackClassificationMapping.SerializeDetectedTags(a.Tags),
                    WorkflowStatus = a.Status,
                    CreatedAt = a.SubmittedUtc,
                    OffersOptOut = guest?.MarketingPreference
                        == LocationGuestMarketingPreference.OptedOut,
                }
            );
        }

        // Pad to 426 with unique comments and distribution targets.
        var positive = 276 - rows.Count(r => r.Sentiment == FeedbackSentiment.Positive);
        var neutral = 82 - rows.Count(r => r.Sentiment == FeedbackSentiment.Neutral);
        var negative = 68 - rows.Count(r => r.Sentiment == FeedbackSentiment.Negative);
        var newLeft = 23 - rows.Count(r => r.WorkflowStatus == FeedbackWorkflowStatus.New);
        var inProgressLeft = 12 - rows.Count(r => r.WorkflowStatus == FeedbackWorkflowStatus.InProgress);

        var guestList = guestsByName.Values.ToList();
        var qrIds = qrByKey.Values.ToArray();
        var padCount = DemoSeedConstants.TargetFeedback - rows.Count;
        for (var i = 0; i < padCount; i++)
        {
            FeedbackSentiment sentiment;
            if (positive > 0)
            {
                sentiment = FeedbackSentiment.Positive;
                positive--;
            }
            else if (neutral > 0)
            {
                sentiment = FeedbackSentiment.Neutral;
                neutral--;
            }
            else
            {
                sentiment = FeedbackSentiment.Negative;
                negative = Math.Max(0, negative - 1);
            }

            FeedbackWorkflowStatus workflow;
            if (newLeft > 0 && sentiment != FeedbackSentiment.Positive)
            {
                workflow = FeedbackWorkflowStatus.New;
                newLeft--;
            }
            else if (inProgressLeft > 0 && sentiment == FeedbackSentiment.Negative)
            {
                workflow = FeedbackWorkflowStatus.InProgress;
                inProgressLeft--;
            }
            else
            {
                workflow = FeedbackWorkflowStatus.Resolved;
            }

            var guest = guestList[i % guestList.Count];
            var day = (i % 89) + 1;
            var created = DemoSeedConstants.SeedNowUtc.AddDays(-day).AddHours(-(i % 20)).AddMinutes(i % 47);
            var baseComment = CommentBank[i % CommentBank.Length];
            var comment = $"{baseComment} (visit note {i + 1} · {created:dd MMM HH:mm}).";

            rows.Add(
                new Feedback
                {
                    RestaurantLocationId = locationId,
                    QrCodeId = qrIds[i % qrIds.Length],
                    LocationGuestId = guest.Id,
                    GuestName = guest.Name,
                    GuestContact = guest.MasterGuest?.Email
                        ?? $"pad.{i}@{DemoSeedConstants.SeedEmailDomain}",
                    ContactType = ContactType.Email,
                    Comment = comment,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = sentiment,
                    ClassifiedAt = created.AddMinutes(3),
                    DetectedTagsJson = FeedbackClassificationMapping.SerializeDetectedTags(
                        BuildTags(i, sentiment)
                    ),
                    WorkflowStatus = workflow,
                    CreatedAt = created,
                }
            );
        }

        AlignNeedsAttentionLast30(rows);

        db.Feedbacks.AddRange(rows);
        await db.SaveChangesAsync();
        return rows;
    }

    /// <summary>
    /// Needs attention = Succeeded Negative ∧ not Resolved. Target 44 in Last 30 days.
    /// </summary>
    private static void AlignNeedsAttentionLast30(List<Feedback> rows)
    {
        var cutoff = DemoSeedConstants.SeedNowUtc.AddDays(-30);
        bool IsNeeds(Feedback f) =>
            f.ClassificationStatus == ClassificationStatus.Succeeded
            && f.Sentiment == FeedbackSentiment.Negative
            && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
            && f.CreatedAt >= cutoff;

        var needs = rows.Where(IsNeeds).ToList();
        var target = DemoSeedConstants.TargetNeedsAttention;

        if (needs.Count < target)
        {
            var candidates = rows
                .Where(f =>
                    f.CreatedAt >= cutoff
                    && f.Sentiment == FeedbackSentiment.Negative
                    && f.WorkflowStatus == FeedbackWorkflowStatus.Resolved
                )
                .OrderByDescending(f => f.CreatedAt)
                .Take(target - needs.Count)
                .ToList();
            foreach (var f in candidates)
            {
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
    }

    private static async Task<LocationGuest> EnsureNamedGuestAsync(
        ApplicationDbContext db,
        int locationId,
        string name,
        Dictionary<string, LocationGuest> map
    )
    {
        var location = await db.RestaurantLocations.FindAsync(locationId)
            ?? throw new InvalidOperationException("Location missing.");
        var email = $"{Slug(name)}@{DemoSeedConstants.SeedEmailDomain}";
        var master = new MasterGuest
        {
            RestaurantId = location.RestaurantId,
            Email = email,
            NormalizedEmail = email,
            CreatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-50),
        };
        db.MasterGuests.Add(master);
        var lg = new LocationGuest
        {
            MasterGuest = master,
            RestaurantLocationId = locationId,
            Name = name,
            MarketingPreference = LocationGuestMarketingPreference.Allowed,
            CreatedAt = master.CreatedAt,
        };
        db.LocationGuests.Add(lg);
        await db.SaveChangesAsync();
        map[name] = lg;
        return lg;
    }

    private static DetectedTag[] BuildTags(int i, FeedbackSentiment sentiment)
    {
        var pool = new[]
        {
            DetectedTag.FoodQuality,
            DetectedTag.Service,
            DetectedTag.WaitTime,
            DetectedTag.Atmosphere,
            DetectedTag.Value,
            DetectedTag.Cleanliness,
            DetectedTag.Billing,
            DetectedTag.AllergiesDietary,
            DetectedTag.BookingSeating,
        };
        if (sentiment == FeedbackSentiment.Negative && i % 5 == 0)
        {
            return [pool[i % pool.Length], pool[(i + 2) % pool.Length]];
        }

        return [pool[i % pool.Length]];
    }

    private static DateTime At(int month, int day, int hour, int minute) =>
        new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static string Slug(string name) =>
        name.Trim().ToLowerInvariant().Replace(' ', '.').Replace("'", "");
}

using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

/// <summary>
/// Ops CLI: seed Location Guests + succeeded Feedback onto an existing Pilot
/// restaurant resolved by owner email. No Offers, Campaigns, or QR minting.
/// </summary>
static class Program
{
    private const string SeedEmailDomain = "qa-seed.tummly.invalid";

    private static readonly string[] GuestFirstNames =
    [
        "Amelia",
        "James",
        "Priya",
        "Oliver",
        "Sofia",
        "Noah",
        "Mia",
        "Ethan",
        "Isla",
        "Lucas",
        "Ava",
        "Leo",
        "Chloe",
        "Harry",
        "Grace",
        "Arthur",
        "Emily",
        "George",
        "Freya",
        "Oscar",
    ];

    private static readonly string[] GuestLastNames =
    [
        "Hart",
        "Patel",
        "Nguyen",
        "Brooks",
        "Singh",
        "Walsh",
        "Khan",
        "Murphy",
        "Chen",
        "Reed",
    ];

    private static readonly DetectedTag[] TagPool =
    [
        DetectedTag.FoodQuality,
        DetectedTag.Service,
        DetectedTag.WaitTime,
        DetectedTag.Cleanliness,
        DetectedTag.Value,
        DetectedTag.Atmosphere,
        DetectedTag.Billing,
        DetectedTag.AllergiesDietary,
        DetectedTag.BookingSeating,
    ];

    private static readonly string[] PositiveComments =
    [
        "Food was excellent and the team were welcoming.",
        "Great atmosphere and a quick turnaround.",
        "Loved the main course — will come back soon.",
        "Service felt attentive without being pushy.",
        "Really good value for a weekday lunch.",
    ];

    private static readonly string[] NeutralComments =
    [
        "Meal was fine overall, nothing stood out.",
        "Average wait and standard portions.",
        "Decent visit; seating was a bit tight.",
        "Food ok, service ok — a regular lunch.",
        "Nothing wrong, but nothing memorable either.",
    ];

    private static readonly string[] NegativeComments =
    [
        "Waited too long and the dish arrived cold.",
        "Service felt rushed and missed our allergen note.",
        "Bill had an unexpected charge we had to query.",
        "Table was not ready despite a booking.",
        "Food quality was below what we expected.",
    ];

    static async Task<int> Main(string[] args)
    {
        var email = GetArg(args, "--email");
        var dryRun = HasFlag(args, "--dry-run");
        var replace = HasFlag(args, "--replace");
        var guestCount = ParseIntArg(args, "--guests", 20);
        var feedbackCount = ParseIntArg(args, "--feedback", 36);

        if (string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine(
                "Usage: QaPilotDemoSeed --email owner@example.com [--dry-run] [--replace] [--guests 20] [--feedback 36]"
            );
            Console.Error.WriteLine(
                "Connection: set ConnectionStrings__DefaultConnection (or CONNECTION_STRING)."
            );
            return 1;
        }

        if (guestCount < 1 || feedbackCount < 1)
        {
            Console.Error.WriteLine("--guests and --feedback must be >= 1.");
            return 1;
        }

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine(
                "ConnectionStrings__DefaultConnection (or CONNECTION_STRING) is required."
            );
            return 1;
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new ApplicationDbContext(options);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Email != null
                && u.Email.ToLower() == normalizedEmail
            );

        if (user is null)
        {
            Console.Error.WriteLine($"No User found for email: {email}");
            return 1;
        }

        var restaurant = await db.Restaurants.AsNoTracking()
            .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

        if (restaurant is null)
        {
            Console.Error.WriteLine(
                $"No Restaurant owned by User Id={user.Id} ({email})."
            );
            return 1;
        }

        var billing = await db.BillingAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.RestaurantId == restaurant.Id);

        if (billing is null)
        {
            Console.Error.WriteLine(
                $"No BillingAccount for Restaurant Id={restaurant.Id}."
            );
            return 1;
        }

        if (!string.Equals(
                billing.SubscriptionPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.Ordinal
            ))
        {
            Console.Error.WriteLine(
                $"Restaurant Id={restaurant.Id} plan is '{billing.SubscriptionPlan}', expected '{BillingSubscriptionPlans.Pilot}'."
            );
            return 1;
        }

        var locations = await db.RestaurantLocations.AsNoTracking()
            .Where(l => l.RestaurantId == restaurant.Id)
            .OrderBy(l => l.Id)
            .ToListAsync();

        if (locations.Count == 0)
        {
            Console.Error.WriteLine(
                $"Restaurant Id={restaurant.Id} has no Owned locations."
            );
            return 1;
        }

        var locationIds = locations.Select(l => l.Id).ToList();
        var activeQrCodes = await db.QrCodes.AsNoTracking()
            .Where(q =>
                locationIds.Contains(q.RestaurantLocationId)
                && q.Status == QrCodeStatus.Active
            )
            .OrderBy(q => q.Id)
            .ToListAsync();
        var qrByLocation = activeQrCodes
            .GroupBy(q => q.RestaurantLocationId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        foreach (var location in locations)
        {
            if (!qrByLocation.ContainsKey(location.Id))
            {
                Console.Error.WriteLine(
                    $"Location Id={location.Id} ({location.LocationName}) has no Active QrCode."
                );
                return 1;
            }
        }

        var existingSeedMasters = await db.MasterGuests.AsNoTracking()
            .CountAsync(m =>
                m.RestaurantId == restaurant.Id
                && m.NormalizedEmail != null
                && m.NormalizedEmail.EndsWith("@" + SeedEmailDomain)
            );

        Console.WriteLine($"Owner: {user.Email} (User Id={user.Id})");
        Console.WriteLine(
            $"Restaurant: {restaurant.Name} (Id={restaurant.Id}, plan=Pilot)"
        );
        Console.WriteLine(
            $"Locations: {string.Join(", ", locations.Select(l => $"{l.LocationName}#{l.Id}"))}"
        );
        Console.WriteLine(
            $"Plan: guests={guestCount}/location, feedback={feedbackCount}/location"
        );
        Console.WriteLine($"Existing seed MasterGuests: {existingSeedMasters}");

        if (dryRun)
        {
            Console.WriteLine("Dry-run only. No writes.");
            return 0;
        }

        if (existingSeedMasters > 0 && !replace)
        {
            Console.Error.WriteLine(
                "Seed data already exists. Pass --replace to delete and reseed."
            );
            return 1;
        }

        if (replace && existingSeedMasters > 0)
        {
            var removed = await RemoveSeedAsync(db, restaurant.Id, locationIds);
            Console.WriteLine(
                $"Removed prior seed: feedback={removed.Feedback}, locationGuests={removed.LocationGuests}, masters={removed.Masters}"
            );
        }

        var totalGuests = 0;
        var totalFeedback = 0;

        foreach (var location in locations)
        {
            var qrCodeId = qrByLocation[location.Id];
            var (guests, feedback) = await SeedLocationAsync(
                db,
                restaurant.Id,
                location.Id,
                qrCodeId,
                guestCount,
                feedbackCount
            );
            totalGuests += guests;
            totalFeedback += feedback;
            Console.WriteLine(
                $"Location {location.LocationName}#{location.Id}: guests={guests}, feedback={feedback}"
            );
        }

        Console.WriteLine(
            $"Done. Inserted guests={totalGuests}, feedback={totalFeedback}."
        );
        return 0;
    }

    private static async Task<(int Feedback, int LocationGuests, int Masters)>
        RemoveSeedAsync(
            ApplicationDbContext db,
            int restaurantId,
            IReadOnlyList<int> locationIds
        )
    {
        var seedMasters = await db.MasterGuests
            .Where(m =>
                m.RestaurantId == restaurantId
                && m.NormalizedEmail != null
                && m.NormalizedEmail.EndsWith("@" + SeedEmailDomain)
            )
            .ToListAsync();

        var masterIds = seedMasters.Select(m => m.Id).ToList();
        var seedLocationGuests = await db.LocationGuests
            .Where(lg => masterIds.Contains(lg.MasterGuestId))
            .ToListAsync();
        var locationGuestIds = seedLocationGuests.Select(lg => lg.Id).ToList();

        var seedFeedback = await db.Feedbacks
            .Where(f =>
                locationIds.Contains(f.RestaurantLocationId)
                && (
                    (f.LocationGuestId != null
                        && locationGuestIds.Contains(f.LocationGuestId.Value))
                    || (f.GuestContact != null
                        && f.GuestContact.EndsWith("@" + SeedEmailDomain))
                )
            )
            .ToListAsync();

        db.Feedbacks.RemoveRange(seedFeedback);
        db.LocationGuests.RemoveRange(seedLocationGuests);
        db.MasterGuests.RemoveRange(seedMasters);
        await db.SaveChangesAsync();

        return (seedFeedback.Count, seedLocationGuests.Count, seedMasters.Count);
    }

    private static async Task<(int Guests, int Feedback)> SeedLocationAsync(
        ApplicationDbContext db,
        int restaurantId,
        int locationId,
        int qrCodeId,
        int guestCount,
        int feedbackCount
    )
    {
        var now = DateTime.UtcNow;
        var locationGuests = new List<LocationGuest>(guestCount);
        var masters = new List<MasterGuest>(guestCount);

        for (var i = 0; i < guestCount; i++)
        {
            var first = GuestFirstNames[i % GuestFirstNames.Length];
            var last = GuestLastNames[i % GuestLastNames.Length];
            var name = $"{first} {last}";
            var slug = $"{first}.{last}.{locationId}.{i + 1}"
                .ToLowerInvariant();
            var channel = i % 3;
            var email = $"{slug}@{SeedEmailDomain}";
            string? mobile = null;
            string? normalizedPhone = null;

            // Always keep a seed-domain email for idempotent --replace detection.
            // Channel mix: email-only / mobile+email / both (Feedback ContactType varies).
            if (channel is 1 or 2)
            {
                // Deterministic UK mobile display + digits-only key.
                var suffix = ((locationId * 100) + i) % 100_000;
                var digits = $"07700{suffix:D5}";
                mobile = digits;
                normalizedPhone = new string(
                    digits.Where(char.IsDigit).ToArray()
                );
            }

            var marketing = (i % 3) switch
            {
                0 => LocationGuestMarketingPreference.Allowed,
                1 => LocationGuestMarketingPreference.OptedOut,
                _ => LocationGuestMarketingPreference.NotRecorded,
            };

            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = email,
                NormalizedEmail = email.ToLowerInvariant(),
                Mobile = mobile,
                NormalizedPhone = normalizedPhone,
                CreatedAt = now.AddDays(-(i % 28) - 1),
            };
            masters.Add(master);

            locationGuests.Add(
                new LocationGuest
                {
                    MasterGuest = master,
                    RestaurantLocationId = locationId,
                    Name = name,
                    MarketingPreference = marketing,
                    CreatedAt = master.CreatedAt,
                }
            );
        }

        db.MasterGuests.AddRange(masters);
        db.LocationGuests.AddRange(locationGuests);
        await db.SaveChangesAsync();

        var feedbackRows = new List<Feedback>(feedbackCount);
        for (var i = 0; i < feedbackCount; i++)
        {
            var sentiment = (i % 3) switch
            {
                0 => FeedbackSentiment.Positive,
                1 => FeedbackSentiment.Neutral,
                _ => FeedbackSentiment.Negative,
            };

            var comment = sentiment switch
            {
                FeedbackSentiment.Positive =>
                    PositiveComments[i % PositiveComments.Length],
                FeedbackSentiment.Neutral =>
                    NeutralComments[i % NeutralComments.Length],
                _ => NegativeComments[i % NegativeComments.Length],
            };

            var tags = BuildTags(i);
            var createdAt = now.AddDays(-(i % 30)).AddHours(-(i % 12));
            var linked = i % 10 != 0;
            LocationGuest? guest = null;
            string guestName;
            string guestContact;
            ContactType contactType;

            if (linked)
            {
                guest = locationGuests[i % locationGuests.Count];
                guestName = guest.Name;
                var master = guest.MasterGuest!;
                // Prefer phone contact on odd linked rows when mobile exists.
                if (i % 2 == 1 && !string.IsNullOrEmpty(master.Mobile))
                {
                    guestContact = master.Mobile;
                    contactType = ContactType.Phone;
                }
                else
                {
                    guestContact = master.Email!;
                    contactType = ContactType.Email;
                }
            }
            else
            {
                guestName = $"Walk-in {i + 1}";
                guestContact = $"walkin.{locationId}.{i + 1}@{SeedEmailDomain}";
                contactType = ContactType.Email;
            }

            feedbackRows.Add(
                new Feedback
                {
                    RestaurantLocationId = locationId,
                    QrCodeId = qrCodeId,
                    LocationGuestId = guest?.Id,
                    GuestName = guestName,
                    GuestContact = guestContact,
                    ContactType = contactType,
                    Comment = comment,
                    OffersOptOut =
                        guest?.MarketingPreference
                        == LocationGuestMarketingPreference.OptedOut,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = sentiment,
                    ClassifiedAt = createdAt.AddMinutes(5),
                    DetectedTagsJson =
                        FeedbackClassificationMapping.SerializeDetectedTags(tags),
                    WorkflowStatus = FeedbackWorkflowStatus.New,
                    CreatedAt = createdAt,
                }
            );
        }

        db.Feedbacks.AddRange(feedbackRows);
        await db.SaveChangesAsync();

        return (locationGuests.Count, feedbackRows.Count);
    }

    private static IReadOnlyList<DetectedTag> BuildTags(int index)
    {
        if (index % 11 == 0)
        {
            return [DetectedTag.Other];
        }

        var primary = TagPool[index % TagPool.Length];
        if (index % 4 == 0)
        {
            var secondary = TagPool[(index + 3) % TagPool.Length];
            if (secondary == primary)
            {
                return [primary];
            }

            return [primary, secondary];
        }

        return [primary];
    }

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static bool HasFlag(string[] args, string name)
        => args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    private static int ParseIntArg(string[] args, string name, int fallback)
    {
        var raw = GetArg(args, name);
        if (raw is null)
        {
            return fallback;
        }

        if (!int.TryParse(raw, out var value))
        {
            throw new ArgumentException($"Invalid integer for {name}: {raw}");
        }

        return value;
    }
}

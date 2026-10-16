using Microsoft.EntityFrameworkCore;
using ProductDemoSeed;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        var dryRun = HasFlag(args, "--dry-run");
        var replace = HasFlag(args, "--replace");

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? TryLoadConnectionFromBackendEnv();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine(
                "ConnectionStrings__DefaultConnection (or CONNECTION_STRING) is required."
            );
            Console.Error.WriteLine(
                "Usage: dotnet run --project backend/tools/ProductDemoSeed -- [--replace] [--dry-run]"
            );
            Console.Error.WriteLine(
                "Or set ConnectionStrings__DefaultConnection in backend/TummlyBackend/.env"
            );
            return 1;
        }

        Console.WriteLine($"ProductDemoSeed → {DemoSeedConstants.RestaurantName}");
        Console.WriteLine($"Owner email: {DemoSeedConstants.OwnerEmail}");
        Console.WriteLine($"Replace: {replace}  Dry-run: {dryRun}");

        if (dryRun)
        {
            Console.WriteLine("Dry-run: no database writes.");
            return 0;
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new ApplicationDbContext(options);

        var (user, restaurant, location) = await AccountBootstrap.EnsureAccountAsync(db);
        Console.WriteLine(
            $"Account ready: user={user.Id} restaurant={restaurant.Id} location={location.Id}"
        );

        if (replace)
        {
            Console.WriteLine("Wiping prior ProductDemoSeed rows for this restaurant…");
            await DemoDataWipe.WipeAsync(db, restaurant.Id, location.Id, user.Id);
        }

        await BrandLogoSeeder.SeedAsync(db, restaurant);

        var placements = await CaptureSeeder.EnsurePlacementsAsync(db, location);
        await CaptureSeeder.SeedScansAsync(db, location.Id, placements);
        Console.WriteLine("Capture placements + scan events seeded.");

        var (guestsByName, feedback) = await GuestFeedbackSeeder.SeedAsync(
            db,
            restaurant.Id,
            location.Id,
            placements
        );
        Console.WriteLine(
            $"Guests={guestsByName.Count} (location total may be higher). Feedback seeded={feedback.Count}."
        );

        await EngagementSeeder.SeedAsync(
            db,
            location.Id,
            user.Id,
            guestsByName,
            feedback
        );
        Console.WriteLine("Recovery / Offers / Campaigns seeded.");

        await WeeklyBriefSeeder.SeedAsync(
            db,
            location.Id,
            user.Id,
            location.LocationName
        );
        Console.WriteLine("Weekly Brief + notification seeded.");

        var guestTotal = await db.LocationGuests.CountAsync(g =>
            g.RestaurantLocationId == location.Id
        );
        var feedbackTotal = await db.Feedbacks.CountAsync(f =>
            f.RestaurantLocationId == location.Id
        );
        var scanTotal = await db.QrScanEvents.CountAsync(e =>
            e.RestaurantLocationId == location.Id
        );
        var formOpenTotal = await db.QrFormOpenEvents.CountAsync(e =>
            e.RestaurantLocationId == location.Id
        );
        var offerTotal = await db.CatalogOffers.CountAsync(o =>
            o.RestaurantLocationId == location.Id
        );
        var campaignTotal = await db.Campaigns.CountAsync(c =>
            c.RestaurantLocationId == location.Id
        );
        var briefTotal = await db.WeeklyBriefs.CountAsync(w => w.LocationId == location.Id);
        var needsAttention = await db.Feedbacks.CountAsync(f =>
            f.RestaurantLocationId == location.Id
            && f.ClassificationStatus == ClassificationStatus.Succeeded
            && f.Sentiment == FeedbackSentiment.Negative
            && f.WorkflowStatus != FeedbackWorkflowStatus.Resolved
            && f.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
        );
        var recoveriesStarted = await db.FeedbackGuestResponses.CountAsync(r =>
            r.Feedback!.RestaurantLocationId == location.Id
            && r.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
        );
        var recoveriesCompleted = await db.FeedbackRecoveryCompletions.CountAsync(c =>
            c.Feedback!.RestaurantLocationId == location.Id
            && c.CreatedAt >= DemoSeedConstants.SeedNowUtc.AddDays(-30)
        );
        var aiUsed = await db.CreditLedgerEntries
            .Where(row =>
                row.RestaurantId == restaurant.Id
                && row.Channel == CreditChannels.Ai
                && row.EntryType == CreditLedgerEntryTypes.Consumption
            )
            .SumAsync(row => (int?)row.Quantity) ?? 0;

        Console.WriteLine("--- Totals ---");
        Console.WriteLine($"LocationGuests: {guestTotal} (target {DemoSeedConstants.TargetGuests})");
        Console.WriteLine($"Feedback: {feedbackTotal} (target {DemoSeedConstants.TargetFeedback})");
        Console.WriteLine($"NeedsAttention L30: {needsAttention} (target {DemoSeedConstants.TargetNeedsAttention})");
        Console.WriteLine($"Recoveries started/completed: {recoveriesStarted}/{recoveriesCompleted} (target {DemoSeedConstants.TargetRecoveriesStarted}/{DemoSeedConstants.TargetRecoveriesCompleted})");
        Console.WriteLine($"QrScanEvents: {scanTotal} (target {DemoSeedConstants.TargetScans})");
        Console.WriteLine($"QrFormOpenEvents: {formOpenTotal} (target {DemoSeedConstants.TargetFormOpens})");
        Console.WriteLine($"CatalogOffers: {offerTotal}");
        Console.WriteLine($"Campaigns: {campaignTotal}");
        Console.WriteLine($"WeeklyBriefs: {briefTotal} (target 4)");
        Console.WriteLine($"AI consumptions: {aiUsed} (target {DemoSeedConstants.TargetAiActionsUsed})");
        Console.WriteLine($"BrandLogoObjectKey: {restaurant.BrandLogoObjectKey}");
        Console.WriteLine();
        Console.WriteLine(
            $"Set DemoSession:OwnerEmail={DemoSeedConstants.OwnerEmail} and restart the API."
        );
        Console.WriteLine("Then POST /api/auth/demo-session → Mavero Kitchen JWT.");
        return 0;
    }

    private static bool HasFlag(string[] args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Local Docker SQL: prefer ConnectionStrings__DefaultConnection from
    /// backend/TummlyBackend/.env, else build from MSSQL_SA_PASSWORD in
    /// backend/TummlyDb/vps/.env.
    /// </summary>
    private static string? TryLoadConnectionFromBackendEnv()
    {
        // Prefer Docker compose SA password (matches tummly-mssql), then backend .env.
        var saPassword = ReadEnvValue(
            [
                Path.GetFullPath(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "backend",
                        "TummlyDb",
                        "vps",
                        ".env"
                    )
                ),
                Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "TummlyDb",
                        "vps",
                        ".env"
                    )
                ),
            ],
            "MSSQL_SA_PASSWORD"
        );

        if (!string.IsNullOrWhiteSpace(saPassword))
        {
            return $"Server=localhost,1433;Database=TummlyDB;User Id=sa;Password={saPassword};TrustServerCertificate=True;Encrypt=True;";
        }

        return ReadEnvValue(
            [
                Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "TummlyBackend",
                        ".env"
                    )
                ),
                Path.GetFullPath(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "backend",
                        "TummlyBackend",
                        ".env"
                    )
                ),
            ],
            "ConnectionStrings__DefaultConnection"
        );
    }

    private static string? ReadEnvValue(IEnumerable<string> paths, string key)
    {
        var prefix = key + "=";
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = line[prefix.Length..].Trim().Trim('\'').Trim('"');
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }
}

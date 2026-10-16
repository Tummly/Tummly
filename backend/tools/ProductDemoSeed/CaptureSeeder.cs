using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace ProductDemoSeed;

internal static class CaptureSeeder
{
    public sealed record PlacementBundle(
        QrCode TableTent,
        QrCode OfferCard,
        QrCode WindowSticker,
        QrCode SmartGuestLink,
        QrCode? Paused
    );

    public static async Task<PlacementBundle> EnsurePlacementsAsync(
        ApplicationDbContext db,
        RestaurantLocation location
    )
    {
        async Task<QrCode> EnsureAsync(
            QrType type,
            string? linkName,
            QrCodeStatus status,
            string tokenSuffix
        )
        {
            var existing = await db.QrCodes.FirstOrDefaultAsync(q =>
                q.RestaurantLocationId == location.Id
                && q.QrType == type
                && (linkName == null
                    || q.LinkName == linkName
                    || q.NormalizedLinkName == linkName.ToLowerInvariant())
            );

            if (existing != null)
            {
                existing.Status = status;
                if (linkName != null)
                {
                    existing.LinkName = linkName;
                    existing.NormalizedLinkName = linkName.ToLowerInvariant();
                }
                return existing;
            }

            var row = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = type,
                Token = $"demo-{tokenSuffix}-{Guid.NewGuid():N}"[..32],
                Status = status,
                LinkName = linkName,
                NormalizedLinkName = linkName?.ToLowerInvariant(),
                CreatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-90),
                CreatedByDisplayName = DemoSeedConstants.OwnerFullName,
            };
            db.QrCodes.Add(row);
            return row;
        }

        var tableTent = await EnsureAsync(
            QrType.TableTent,
            null,
            QrCodeStatus.Active,
            "table-tent"
        );
        tableTent.InternalDescription = "Table Tent - Main Dining";

        var offerCard = await EnsureAsync(
            QrType.OfferCard,
            null,
            QrCodeStatus.Active,
            "offer-card"
        );
        offerCard.InternalDescription = "Offer Card - Counter";

        var window = await EnsureAsync(
            QrType.WindowSticker,
            null,
            QrCodeStatus.Active,
            "window"
        );
        window.InternalDescription = "Window Sticker - Front";

        var smart = await EnsureAsync(
            QrType.DigitalGuestLink,
            "Smart Guest Link",
            QrCodeStatus.Active,
            "smart"
        );
        smart.Channel = DigitalGuestLinkChannel.Website;
        smart.InternalDescription = "Smart Guest Link";

        var paused = await EnsureAsync(
            QrType.DigitalGuestLink,
            "Old counter link",
            QrCodeStatus.Paused,
            "paused"
        );
        paused.Channel = DigitalGuestLinkChannel.Other;

        await db.SaveChangesAsync();
        return new PlacementBundle(tableTent, offerCard, window, smart, paused);
    }

    public static async Task SeedScansAsync(
        ApplicationDbContext db,
        int locationId,
        PlacementBundle placements
    )
    {
        var existing = await db.QrScanEvents.CountAsync(e =>
            e.RestaurantLocationId == locationId
        );
        if (existing >= DemoSeedConstants.TargetScans)
        {
            return;
        }

        // Spec §6 Last-30-day placement mix (approximate across 90 days with heavier last 30).
        var plan = new (QrCode Qr, int Count)[]
        {
            (placements.TableTent, 840),
            (placements.OfferCard, 417),
            (placements.WindowSticker, 388),
            (placements.SmartGuestLink, 197),
        };

        var toAdd = DemoSeedConstants.TargetScans - existing;
        var events = new List<QrScanEvent>(toAdd);
        var now = DemoSeedConstants.SeedNowUtc;
        var i = 0;
        foreach (var (qr, count) in plan)
        {
            var share = (int)Math.Round(toAdd * (count / (double)DemoSeedConstants.TargetScans));
            for (var n = 0; n < share && events.Count < toAdd; n++, i++)
            {
                var daysAgo = n % 30;
                events.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        CreatedAt = now.AddDays(-daysAgo).AddMinutes(-(i % 1400)),
                    }
                );
            }
        }

        while (events.Count < toAdd)
        {
            events.Add(
                new QrScanEvent
                {
                    RestaurantLocationId = locationId,
                    QrCodeId = placements.TableTent.Id,
                    CreatedAt = now.AddDays(-(events.Count % 30)).AddMinutes(-events.Count),
                }
            );
        }

        db.QrScanEvents.AddRange(events);
        await db.SaveChangesAsync();

        await SeedFormOpensAsync(db, locationId, placements);
    }

    public static async Task SeedFormOpensAsync(
        ApplicationDbContext db,
        int locationId,
        PlacementBundle placements
    )
    {
        var existing = await db.QrFormOpenEvents.CountAsync(e =>
            e.RestaurantLocationId == locationId
        );
        if (existing >= DemoSeedConstants.TargetFormOpens)
        {
            return;
        }

        var plan = new (QrCode Qr, int Count)[]
        {
            (placements.TableTent, 696),
            (placements.OfferCard, 342),
            (placements.WindowSticker, 317),
            (placements.SmartGuestLink, 161),
        };

        var toAdd = DemoSeedConstants.TargetFormOpens - existing;
        var events = new List<QrFormOpenEvent>(toAdd);
        var now = DemoSeedConstants.SeedNowUtc;
        var i = 0;
        foreach (var (qr, count) in plan)
        {
            var share = (int)Math.Round(
                toAdd * (count / (double)DemoSeedConstants.TargetFormOpens)
            );
            for (var n = 0; n < share && events.Count < toAdd; n++, i++)
            {
                events.Add(
                    new QrFormOpenEvent
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        CreatedAt = now.AddDays(-(n % 30)).AddMinutes(-(i % 1300)),
                    }
                );
            }
        }

        while (events.Count < toAdd)
        {
            events.Add(
                new QrFormOpenEvent
                {
                    RestaurantLocationId = locationId,
                    QrCodeId = placements.TableTent.Id,
                    CreatedAt = now.AddDays(-(events.Count % 30)).AddMinutes(-events.Count),
                }
            );
        }

        db.QrFormOpenEvents.AddRange(events);
        await db.SaveChangesAsync();
    }
}

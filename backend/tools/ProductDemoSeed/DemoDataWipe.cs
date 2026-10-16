using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Services;

namespace ProductDemoSeed;

internal static class DemoDataWipe
{
    /// <summary>
    /// Removes prior ProductDemoSeed rows for this restaurant/location only.
    /// Keeps Owner, Restaurant, Location, Billing, and QR placement rows.
    /// </summary>
    public static async Task WipeAsync(
        ApplicationDbContext db,
        int restaurantId,
        int locationId,
        int ownerUserId
    )
    {
        var offerIds = await db.CatalogOffers
            .Where(o => o.RestaurantLocationId == locationId)
            .Select(o => o.Id)
            .ToListAsync();

        if (offerIds.Count > 0)
        {
            await db.OfferIssues
                .Where(i => offerIds.Contains(i.CatalogOfferId))
                .ExecuteDeleteAsync();
        }

        var campaignIds = await db.Campaigns
            .Where(c => c.RestaurantLocationId == locationId)
            .Select(c => c.Id)
            .ToListAsync();

        if (campaignIds.Count > 0)
        {
            await db.CampaignRecipientDeliveries
                .Where(d => campaignIds.Contains(d.CampaignId))
                .ExecuteDeleteAsync();
            await db.CampaignFrozenRecipients
                .Where(d => campaignIds.Contains(d.CampaignId))
                .ExecuteDeleteAsync();
            await db.Campaigns
                .Where(c => campaignIds.Contains(c.Id))
                .ExecuteDeleteAsync();
        }

        var feedbackIds = await db.Feedbacks
            .Where(f => f.RestaurantLocationId == locationId)
            .Select(f => f.Id)
            .ToListAsync();

        if (feedbackIds.Count > 0)
        {
            await db.FeedbackRecoveryCompletions
                .Where(c => feedbackIds.Contains(c.FeedbackId))
                .ExecuteDeleteAsync();
            await db.FeedbackWorkflowStatusChanges
                .Where(c => feedbackIds.Contains(c.FeedbackId))
                .ExecuteDeleteAsync();
            await db.FeedbackGuestResponses
                .Where(r => feedbackIds.Contains(r.FeedbackId))
                .ExecuteDeleteAsync();
            await db.Feedbacks
                .Where(f => feedbackIds.Contains(f.Id))
                .ExecuteDeleteAsync();
        }

        await db.QrScanEvents
            .Where(e => e.RestaurantLocationId == locationId)
            .ExecuteDeleteAsync();

        await db.QrFormOpenEvents
            .Where(e => e.RestaurantLocationId == locationId)
            .ExecuteDeleteAsync();

        await db.WeeklyBriefs
            .Where(w => w.LocationId == locationId)
            .ExecuteDeleteAsync();

        await db.CatalogOffers
            .Where(o => o.RestaurantLocationId == locationId)
            .ExecuteDeleteAsync();

        var guestIds = await db.LocationGuests
            .Where(g => g.RestaurantLocationId == locationId)
            .Select(g => g.Id)
            .ToListAsync();

        if (guestIds.Count > 0)
        {
            await db.LocationGuestActivityEvents
                .Where(e =>
                    e.LocationGuestId != null && guestIds.Contains(e.LocationGuestId.Value)
                )
                .ExecuteDeleteAsync();
            await db.LocationGuestNotes
                .Where(n => guestIds.Contains(n.LocationGuestId))
                .ExecuteDeleteAsync();
            await db.LocationGuestTags
                .Where(t => guestIds.Contains(t.LocationGuestId))
                .ExecuteDeleteAsync();
            await db.LocationGuestPermissionLedgerEntries
                .Where(e => guestIds.Contains(e.LocationGuestId))
                .ExecuteDeleteAsync();
        }

        await db.LocationGuests
            .Where(g => g.RestaurantLocationId == locationId)
            .ExecuteDeleteAsync();

        await db.MasterGuests
            .Where(m => m.RestaurantId == restaurantId)
            .Where(m => !db.LocationGuests.Any(g => g.MasterGuestId == m.Id))
            .ExecuteDeleteAsync();

        var weekKey = $"monday:{DemoSeedConstants.AnchorWeekStartUtc:yyyy-MM-dd}";
        var dedupe = WeeklyBriefReadyNotifier.DedupeKeyFor(locationId, weekKey);
        await db.Notifications
            .Where(n =>
                n.UserId == ownerUserId
                && n.Type == WeeklyBriefReadyNotifier.NotificationType
                && n.DedupeKey == dedupe
            )
            .ExecuteDeleteAsync();
    }
}

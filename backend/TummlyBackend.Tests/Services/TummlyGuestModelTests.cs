using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services;

public class TummlyGuestModelTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public TummlyGuestModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
            )
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Model_SavesGuestWithContactEventAndMute()
    {
        var now = new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);
        var guest = new TummlyGuest
        {
            FirstName = "Sofia",
            CreatedAt = now,
            UpdatedAt = now,
        };
        guest.Contacts.Add(
            new TummlyGuestContact
            {
                ContactType = TummlyGuestContactTypes.Email,
                NormalizedValue = "sofia@example.com",
                VerifiedAt = now,
                CreatedAt = now,
            }
        );
        guest.MarketingEvents.Add(
            new TummlyGuestMarketingEvent
            {
                Channel = TummlyGuestChannels.Email,
                EventKind = LocationGuestPermissionLedgerEventKinds.Grant,
                Source = LocationGuestPermissionLedgerSources.PreferenceCentre,
                WordingSnapshot = "Tummly offers by email",
                OccurredAt = now,
                CreatedAt = now,
            }
        );
        guest.LocationMutes.Add(
            new TummlyGuestLocationMute
            {
                RestaurantLocationId = 1,
                MutedAt = now,
            }
        );

        _context.TummlyGuests.Add(guest);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context
            .TummlyGuests.Include(g => g.Contacts)
            .Include(g => g.MarketingEvents)
            .Include(g => g.LocationMutes)
            .SingleAsync(g => g.Id == guest.Id);

        Assert.Single(reloaded.Contacts);
        Assert.Single(reloaded.MarketingEvents);
        Assert.Single(reloaded.LocationMutes);
    }

    [Fact]
    public void Model_HasUniqueIndexOnContactTypeAndValue()
    {
        var index = _context
            .Model.FindEntityType(typeof(TummlyGuestContact))!
            .GetIndexes()
            .Single(i =>
                i.Properties.Select(p => p.Name)
                    .SequenceEqual(new[] { "ContactType", "NormalizedValue" })
            );

        Assert.True(index.IsUnique);
    }
}

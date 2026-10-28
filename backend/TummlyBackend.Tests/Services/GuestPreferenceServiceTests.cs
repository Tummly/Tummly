using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.DTOs.GuestPreferences;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="IGuestPreferenceService"/> — read model and consent write
    /// rules for the guest preference centre (D2, D5, D6, A2).
    /// </summary>
    public class GuestPreferenceServiceTests : IDisposable
    {
        private const string Email = "sophie@example.com";
        private const string EmailWording =
            "Email marketing — Receive dining news and offers by Email.";
        private const string SmsWording =
            "SMS marketing — Receive dining news and offers by text.";
        private const string ActivityDescription =
            "Guest withdrew marketing permission at this location.";

        private static readonly DateTime Past = DateTime.UtcNow.AddDays(-30);

        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly IGuestPreferenceService _sut;

        public GuestPreferenceServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _permissions = new LocationGuestPermissionLedgerService(_context);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["PublicApi:BaseUrl"] = "https://api.tummly.test",
                    }
                )
                .Build();
            _sut = new GuestPreferenceService(
                _context,
                new TummlyGuestIdentityService(_context),
                _permissions,
                configuration
            );
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task Overview_EmailOn_FromRestaurantGrantOnly()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var venue = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(venue.Guest, LocationGuestPermissionKind.EmailMarketing, Past);

            var overview = await _sut.GetOverviewAsync(tummlyGuestId, CancellationToken.None);

            Assert.True(overview.EmailOn);
            Assert.False(overview.SmsOn);
            Assert.Equal(1, overview.RestaurantCount);
            var contact = Assert.Single(overview.Contacts);
            Assert.Equal("email", contact.Type);
            Assert.Equal(GuestPreferenceContact.Mask("email", Email), contact.Masked);
            Assert.DoesNotContain(Email, contact.Masked);
            Assert.Null(overview.FirstName);
        }

        [Fact]
        public async Task Overview_EmailOn_FalseWhenNothingGrantedOrTummlyOn()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            await SeedVenueGuestAsync("Venue A", "Main");

            var overview = await _sut.GetOverviewAsync(tummlyGuestId, CancellationToken.None);

            Assert.False(overview.EmailOn);
            Assert.False(overview.SmsOn);
        }

        [Fact]
        public async Task SetChannels_NoKeys_WritesNothing()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var venue = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(venue.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            var ledgerBefore = await _context.LocationGuestPermissionLedgerEntries.CountAsync();
            var eventsBefore = await _context.TummlyGuestMarketingEvents.CountAsync();

            await _sut.SetChannelsAsync(
                tummlyGuestId,
                new GuestPreferenceChannelsRequest(null, null),
                CancellationToken.None
            );

            Assert.Equal(ledgerBefore, await _context.LocationGuestPermissionLedgerEntries.CountAsync());
            Assert.Equal(eventsBefore, await _context.TummlyGuestMarketingEvents.CountAsync());
        }

        [Fact]
        public async Task SetChannels_EmailOff_WithdrawsEverywhereAndTummly()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            var b = await SeedVenueGuestAsync("Venue B", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await GrantAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing, Past.AddMinutes(1));
            await GrantAsync(b.Guest, LocationGuestPermissionKind.EmailMarketing, Past.AddMinutes(2));

            await _sut.SetChannelsAsync(
                tummlyGuestId,
                new GuestPreferenceChannelsRequest(Email: false, Sms: null),
                CancellationToken.None
            );

            var tummlyEvents = await TummlyEventsAsync(tummlyGuestId);
            var emailEvent = Assert.Single(tummlyEvents);
            Assert.Equal(TummlyGuestChannels.Email, emailEvent.Channel);
            Assert.Equal(LocationGuestPermissionLedgerEventKinds.Withdraw, emailEvent.EventKind);
            Assert.Equal(LocationGuestPermissionLedgerSources.PreferenceCentre, emailEvent.Source);
            Assert.Null(emailEvent.WordingSnapshot);

            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(b.Guest, LocationGuestPermissionKind.EmailMarketing)
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                await StateAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing)
            );
            Assert.Equal(
                LocationGuestPermissionLedgerSources.PreferenceCentre,
                (await LedgerAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)).Last().Source
            );

            // B is left with no Granted kind, so the rollup flips to OptedOut.
            var activity = Assert.Single(
                await _context.LocationActivities.ToListAsync(),
                x => x.Kind == LocationActivityKinds.GuestMarketingUnsubscribed
            );
            Assert.Equal(b.Location.Id, activity.LocationId);
            Assert.Equal("Guest", activity.ActorDisplayName);
            Assert.Equal(ActivityDescription, activity.Description);
        }

        [Fact]
        public async Task SetChannels_EmailOn_RestoresOnlyGuestMadeWithdrawals()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();

            // A: granted, then the guest unsubscribed by email link. Restorable.
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await RecordAsync(
                a.Guest,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe,
                Past.AddMinutes(1)
            );

            // B: granted, then a hard bounce. Never restored.
            var b = await SeedVenueGuestAsync("Venue B", "Main");
            await GrantAsync(b.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await RecordAsync(
                b.Guest,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                GuestContactSuppressionSources.Bounce,
                Past.AddMinutes(1)
            );

            // C: SMS only. Email was never granted here, so no email row is created.
            var c = await SeedVenueGuestAsync("Venue C", "Main");
            await GrantAsync(c.Guest, LocationGuestPermissionKind.SmsMarketing, Past);

            await _sut.SetChannelsAsync(
                tummlyGuestId,
                new GuestPreferenceChannelsRequest(Email: true, Sms: null),
                CancellationToken.None
            );

            var restored = await LedgerAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing);
            Assert.Equal(3, restored.Count);
            Assert.Equal(LocationGuestPermissionLedgerEventKinds.Grant, restored.Last().EventKind);
            Assert.Equal(LocationGuestPermissionLedgerSources.PreferenceCentre, restored.Last().Source);
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                await StateAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)
            );

            Assert.Equal(2, (await LedgerAsync(b.Guest, LocationGuestPermissionKind.EmailMarketing)).Count);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(b.Guest, LocationGuestPermissionKind.EmailMarketing)
            );

            Assert.Empty(await LedgerAsync(c.Guest, LocationGuestPermissionKind.EmailMarketing));

            var tummlyEvent = Assert.Single(await TummlyEventsAsync(tummlyGuestId));
            Assert.Equal(TummlyGuestChannels.Email, tummlyEvent.Channel);
            Assert.Equal(LocationGuestPermissionLedgerEventKinds.Grant, tummlyEvent.EventKind);
            Assert.Equal(EmailWording, tummlyEvent.WordingSnapshot);
        }

        [Fact]
        public async Task SetChannels_EmailOn_SkipsMutedLocation()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await _sut.SetLocationMuteAsync(tummlyGuestId, a.Location.Id, muted: true, CancellationToken.None);

            // The mute withdraw uses preference-centre, which would restore if not muted.
            await _sut.SetChannelsAsync(
                tummlyGuestId,
                new GuestPreferenceChannelsRequest(Email: true, Sms: null),
                CancellationToken.None
            );

            var latest = (await LedgerAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)).Last();
            Assert.Equal(LocationGuestPermissionLedgerEventKinds.Withdraw, latest.EventKind);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)
            );
        }

        [Fact]
        public async Task StopAll_WithdrawsBoth_KeepsFeedbackFollowUp()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await GrantAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing, Past.AddMinutes(1));
            await GrantAsync(a.Guest, LocationGuestPermissionKind.FeedbackFollowUp, Past.AddMinutes(2));

            await _sut.StopAllAsync(tummlyGuestId, CancellationToken.None);

            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing)
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                await StateAsync(a.Guest, LocationGuestPermissionKind.FeedbackFollowUp)
            );

            var events = await TummlyEventsAsync(tummlyGuestId);
            Assert.Equal(2, events.Count);
            Assert.All(events, e => Assert.Equal(LocationGuestPermissionLedgerEventKinds.Withdraw, e.EventKind));
            Assert.Equal(
                new[] { TummlyGuestChannels.Email, TummlyGuestChannels.Sms },
                events.Select(e => e.Channel).OrderBy(x => x, StringComparer.Ordinal).ToArray()
            );
        }

        [Fact]
        public async Task StopAll_WritesTummlyWithdraw_EvenWhenNothingGranted()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            await SeedVenueGuestAsync("Venue A", "Main");

            await _sut.StopAllAsync(tummlyGuestId, CancellationToken.None);

            var events = await TummlyEventsAsync(tummlyGuestId);
            Assert.Equal(2, events.Count);
            Assert.All(events, e => Assert.Equal(LocationGuestPermissionLedgerEventKinds.Withdraw, e.EventKind));
        }

        [Fact]
        public async Task MuteLocation_WithdrawsEmailAndSms_AndExcludesFromOverview()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await GrantAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing, Past.AddMinutes(1));

            await _sut.SetLocationMuteAsync(tummlyGuestId, a.Location.Id, muted: true, CancellationToken.None);

            foreach (var kind in new[] { LocationGuestPermissionKind.EmailMarketing, LocationGuestPermissionKind.SmsMarketing })
            {
                var latest = (await LedgerAsync(a.Guest, kind)).Last();
                Assert.Equal(LocationGuestPermissionLedgerEventKinds.Withdraw, latest.EventKind);
                Assert.Equal(LocationGuestPermissionLedgerSources.PreferenceCentre, latest.Source);
            }

            Assert.Single(await _context.TummlyGuestLocationMutes.Where(m => m.TummlyGuestId == tummlyGuestId).ToListAsync());
            Assert.Empty(await TummlyEventsAsync(tummlyGuestId));

            var overview = await _sut.GetOverviewAsync(tummlyGuestId, CancellationToken.None);
            Assert.False(overview.EmailOn);
            Assert.False(overview.SmsOn);
            Assert.Equal(0, overview.RestaurantCount);

            var restaurant = Assert.Single(await _sut.ListRestaurantsAsync(tummlyGuestId, null, CancellationToken.None));
            Assert.True(Assert.Single(restaurant.Locations).Muted);
        }

        [Fact]
        public async Task MuteLocation_Twice_IsIdempotent()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);

            await _sut.SetLocationMuteAsync(tummlyGuestId, a.Location.Id, muted: true, CancellationToken.None);
            var ledgerCount = await _context.LocationGuestPermissionLedgerEntries.CountAsync();
            await _sut.SetLocationMuteAsync(tummlyGuestId, a.Location.Id, muted: true, CancellationToken.None);

            Assert.Equal(ledgerCount, await _context.LocationGuestPermissionLedgerEntries.CountAsync());
            Assert.Single(await _context.TummlyGuestLocationMutes.ToListAsync());
        }

        [Fact]
        public async Task UnmuteLocation_RestoresOnlyChannelsThatAreOn()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await GrantAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing, Past.AddMinutes(1));
            // Tummly email stream is on. Tummly SMS stream is not.
            await RecordTummlyEventAsync(tummlyGuestId, TummlyGuestChannels.Email, LocationGuestPermissionLedgerEventKinds.Grant, Past.AddMinutes(2), EmailWording);

            await _sut.SetLocationMuteAsync(tummlyGuestId, a.Location.Id, muted: true, CancellationToken.None);
            var smsRowsBeforeUnmute = (await LedgerAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing)).Count;

            await _sut.SetLocationMuteAsync(tummlyGuestId, a.Location.Id, muted: false, CancellationToken.None);

            Assert.Empty(await _context.TummlyGuestLocationMutes.ToListAsync());
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                await StateAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing)
            );
            Assert.Equal(smsRowsBeforeUnmute, (await LedgerAsync(a.Guest, LocationGuestPermissionKind.SmsMarketing)).Count);
        }

        [Fact]
        public async Task UnmuteLocation_NotMuted_RestoresNothing()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var a = await SeedVenueGuestAsync("Venue A", "Main");
            await GrantAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await RecordAsync(
                a.Guest,
                LocationGuestPermissionKind.EmailMarketing,
                LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.EmailUnsubscribe,
                Past.AddMinutes(1)
            );
            await RecordTummlyEventAsync(tummlyGuestId, TummlyGuestChannels.Email, LocationGuestPermissionLedgerEventKinds.Grant, Past.AddMinutes(2), EmailWording);
            var rowsBefore = await _context.LocationGuestPermissionLedgerEntries.CountAsync();

            await _sut.SetLocationMuteAsync(tummlyGuestId, a.Location.Id, muted: false, CancellationToken.None);

            Assert.Equal(rowsBefore, await _context.LocationGuestPermissionLedgerEntries.CountAsync());
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(a.Guest, LocationGuestPermissionKind.EmailMarketing)
            );
        }

        [Fact]
        public async Task MuteRestaurant_MutesAllItsMatchedLocations()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var restaurant = await SeedRestaurantAsync("Venue Multi");
            var loc1 = await SeedLocationAsync(restaurant, "Harbour");
            var loc2 = await SeedLocationAsync(restaurant, "Park");
            var g1 = await SeedLocationGuestAsync(loc1, Email);
            var g2 = await SeedLocationGuestAsync(loc2, Email);
            await GrantAsync(g1, LocationGuestPermissionKind.EmailMarketing, Past);
            await GrantAsync(g2, LocationGuestPermissionKind.EmailMarketing, Past);
            var other = await SeedVenueGuestAsync("Venue Other", "Main");
            await GrantAsync(other.Guest, LocationGuestPermissionKind.EmailMarketing, Past);

            await _sut.SetRestaurantMuteAsync(tummlyGuestId, restaurant.Id, muted: true, CancellationToken.None);

            var mutedLocationIds = await _context.TummlyGuestLocationMutes
                .Where(m => m.TummlyGuestId == tummlyGuestId)
                .Select(m => m.RestaurantLocationId)
                .ToListAsync();
            Assert.Equal(2, mutedLocationIds.Count);
            Assert.Contains(loc1.Id, mutedLocationIds);
            Assert.Contains(loc2.Id, mutedLocationIds);
            Assert.DoesNotContain(other.Location.Id, mutedLocationIds);
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(g1, LocationGuestPermissionKind.EmailMarketing)
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                await StateAsync(g2, LocationGuestPermissionKind.EmailMarketing)
            );
        }

        [Fact]
        public async Task MuteLocation_NotInList_Throws()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            await SeedVenueGuestAsync("Venue A", "Main");
            // A location where this guest never granted anything is not on the list.
            var stranger = await SeedVenueGuestAsync("Venue Stranger", "Main", email: "other@example.com");

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.SetLocationMuteAsync(tummlyGuestId, stranger.Location.Id, muted: true, CancellationToken.None)
            );
            Assert.Empty(await _context.TummlyGuestLocationMutes.ToListAsync());
        }

        [Fact]
        public async Task MuteRestaurant_NotInList_Throws()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var stranger = await SeedVenueGuestAsync("Venue Stranger", "Main", email: "other@example.com");

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.SetRestaurantMuteAsync(tummlyGuestId, stranger.Restaurant.Id, muted: true, CancellationToken.None)
            );
        }

        [Fact]
        public async Task MuteLocation_UnknownTummlyGuest_Throws()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.SetLocationMuteAsync(999_999, 1, muted: true, CancellationToken.None)
            );
        }

        [Fact]
        public async Task SetChannels_UnknownTummlyGuest_Throws()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.SetChannelsAsync(
                    999_999,
                    new GuestPreferenceChannelsRequest(Email: false, Sms: false),
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task ListRestaurants_FiltersByQuery()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var alpha = await SeedVenueGuestAsync("Venue Alpha", "Main", logoKey: "brand-logos/alpha.png");
            var beta = await SeedVenueGuestAsync("Venue Beta", "Harbour");
            await GrantAsync(alpha.Guest, LocationGuestPermissionKind.EmailMarketing, Past);
            await GrantAsync(beta.Guest, LocationGuestPermissionKind.EmailMarketing, Past);

            var all = await _sut.ListRestaurantsAsync(tummlyGuestId, null, CancellationToken.None);
            Assert.Equal(new[] { "Venue Alpha", "Venue Beta" }, all.Select(r => r.Name));
            Assert.NotNull(all[0].LogoUrl);
            Assert.StartsWith("https://api.tummly.test/", all[0].LogoUrl);
            Assert.EndsWith("alpha.png", all[0].LogoUrl);
            Assert.Null(all[1].LogoUrl);

            var byRestaurant = await _sut.ListRestaurantsAsync(tummlyGuestId, "ALPHA", CancellationToken.None);
            Assert.Equal(new[] { "Venue Alpha" }, byRestaurant.Select(r => r.Name));

            var byLocation = await _sut.ListRestaurantsAsync(tummlyGuestId, "harbour", CancellationToken.None);
            var only = Assert.Single(byLocation);
            Assert.Equal("Venue Beta", only.Name);
            Assert.Equal(beta.Location.Id, Assert.Single(only.Locations).LocationId);
        }

        [Fact]
        public async Task SetFirstName_TrimsAndStores_WithoutChangingLocationGuestName()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();
            var venue = await SeedVenueGuestAsync("Venue A", "Main");

            await _sut.SetFirstNameAsync(tummlyGuestId, "  Sophie  ", CancellationToken.None);

            var overview = await _sut.GetOverviewAsync(tummlyGuestId, CancellationToken.None);
            Assert.Equal("Sophie", overview.FirstName);
            var lg = await _context.LocationGuests.SingleAsync(x => x.Id == venue.Guest.Id);
            Assert.Equal("Guest", lg.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SetFirstName_Blank_Throws(string firstName)
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.SetFirstNameAsync(tummlyGuestId, firstName, CancellationToken.None)
            );
        }

        [Fact]
        public async Task SetFirstName_TooLong_Throws()
        {
            var tummlyGuestId = await SeedTummlyGuestAsync();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _sut.SetFirstNameAsync(tummlyGuestId, new string('x', 101), CancellationToken.None)
            );
        }

        [Fact]
        public async Task SetFirstName_UnknownTummlyGuest_Throws()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.SetFirstNameAsync(999_999, "Sophie", CancellationToken.None)
            );
        }

        private async Task<int> SeedTummlyGuestAsync()
        {
            var now = DateTime.UtcNow;
            var guest = new TummlyGuest { CreatedAt = now, UpdatedAt = now };
            guest.Contacts.Add(
                new TummlyGuestContact
                {
                    ContactType = TummlyGuestContactTypes.Email,
                    NormalizedValue = Email,
                    VerifiedAt = now,
                    CreatedAt = now,
                }
            );
            _context.TummlyGuests.Add(guest);
            await _context.SaveChangesAsync();
            return guest.Id;
        }

        private async Task<Restaurant> SeedRestaurantAsync(string name, string? logoKey = null)
        {
            var restaurant = new Restaurant
            {
                Name = name,
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
                BrandLogoObjectKey = logoKey,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            return restaurant;
        }

        private async Task<RestaurantLocation> SeedLocationAsync(Restaurant restaurant, string locationName)
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        private async Task<LocationGuest> SeedLocationGuestAsync(RestaurantLocation location, string email)
        {
            var master = new MasterGuest
            {
                RestaurantId = location.RestaurantId,
                Email = email,
                NormalizedEmail = email,
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                MasterGuest = master,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();
            return guest;
        }

        private sealed record Venue(Restaurant Restaurant, RestaurantLocation Location, LocationGuest Guest);

        private async Task<Venue> SeedVenueGuestAsync(
            string restaurantName,
            string locationName,
            string email = Email,
            string? logoKey = null
        )
        {
            var restaurant = await SeedRestaurantAsync(restaurantName, logoKey);
            var location = await SeedLocationAsync(restaurant, locationName);
            var guest = await SeedLocationGuestAsync(location, email);
            return new Venue(restaurant, location, guest);
        }

        private async Task GrantAsync(LocationGuest guest, LocationGuestPermissionKind kind, DateTime occurredAt) =>
            await RecordAsync(
                guest,
                kind,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                occurredAt
            );

        private async Task RecordAsync(
            LocationGuest guest,
            LocationGuestPermissionKind kind,
            string eventKind,
            string source,
            DateTime occurredAt
        )
        {
            _permissions.RecordEvent(guest, guest.RestaurantLocationId, kind, eventKind, source, occurredAt);
            await _context.SaveChangesAsync();
        }

        private async Task RecordTummlyEventAsync(
            int tummlyGuestId,
            string channel,
            string eventKind,
            DateTime occurredAt,
            string? wording
        )
        {
            _context.TummlyGuestMarketingEvents.Add(
                new TummlyGuestMarketingEvent
                {
                    TummlyGuestId = tummlyGuestId,
                    Channel = channel,
                    EventKind = eventKind,
                    Source = LocationGuestPermissionLedgerSources.PreferenceCentre,
                    WordingSnapshot = wording,
                    OccurredAt = occurredAt,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task<LocationGuestPermissionState> StateAsync(
            LocationGuest guest,
            LocationGuestPermissionKind kind
        ) => (await _permissions.GetCurrentStatesAsync(guest.Id))[kind];

        private async Task<List<LocationGuestPermissionLedgerEntry>> LedgerAsync(
            LocationGuest guest,
            LocationGuestPermissionKind kind
        ) =>
            await _context.LocationGuestPermissionLedgerEntries
                .AsNoTracking()
                .Where(e => e.LocationGuestId == guest.Id && e.PermissionKind == kind)
                .OrderBy(e => e.OccurredAt)
                .ThenBy(e => e.Id)
                .ToListAsync();

        private async Task<List<TummlyGuestMarketingEvent>> TummlyEventsAsync(int tummlyGuestId) =>
            await _context.TummlyGuestMarketingEvents
                .AsNoTracking()
                .Where(e => e.TummlyGuestId == tummlyGuestId)
                .OrderBy(e => e.Id)
                .ToListAsync();
    }
}

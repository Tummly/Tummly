using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="TwilioSmsInboundService"/> — signature, STOP keyword, To→restaurant map.
    /// </summary>
    public class TwilioSmsInboundServiceTests : IDisposable
    {
        private const string AuthToken = "twilio_svc_auth_token";
        private const string MappedTo = "+447700900999";
        private const string GuestFrom = "+447700900111";
        private const string RequestUrl =
            "http://localhost/api/webhooks/twilio/sms-inbound";

        private readonly ApplicationDbContext _context;
        private readonly ILocationGuestPermissionLedgerService _permissions;
        private readonly IGuestInitiatedMarketingWithdrawService _withdraw;
        private readonly TwilioSmsInboundService _sut;

        public TwilioSmsInboundServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _permissions = new LocationGuestPermissionLedgerService(_context);
            _withdraw = new GuestInitiatedMarketingWithdrawService(
                _context,
                _permissions
            );

            var settings = Options.Create(
                new TwilioSettings
                {
                    AuthToken = AuthToken,
                    DefaultRegion = "GB",
                    InboundNumberRestaurants =
                    {
                        [MappedTo] = 1,
                    },
                }
            );

            _sut = new TwilioSmsInboundService(
                settings,
                _withdraw,
                NullLogger<TwilioSmsInboundService>.Instance
            );
        }

        [Fact]
        public async Task Handle_Stop_MappedTo_WithdrawsSmsMarketing()
        {
            var seeded = await SeedGrantedSmsGuestAsync(GuestFrom);
            Assert.Equal(1, seeded.RestaurantId);

            var form = BuildForm(GuestFrom, MappedTo, "STOP");
            var signature = Sign(form);

            var status = await _sut.HandleAsync(RequestUrl, form, signature);

            Assert.Equal(TwilioSmsInboundStatus.Accepted, status);
            var states = await _permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
        }

        [Fact]
        public async Task Handle_Stop_FormattedTo_MatchesNormalizedMapKey()
        {
            var seeded = await SeedGrantedSmsGuestAsync(GuestFrom);
            Assert.Equal(1, seeded.RestaurantId);

            // Map key is E.164; Twilio To arrives with spaces — must still resolve.
            var form = BuildForm(GuestFrom, "+44 7700 900999", "STOP");
            var signature = Sign(form);

            var status = await _sut.HandleAsync(RequestUrl, form, signature);

            Assert.Equal(TwilioSmsInboundStatus.Accepted, status);
            var states = await _permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Withdrawn,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
        }

        [Fact]
        public async Task Handle_Stop_UnmappedTo_DoesNotWithdraw()
        {
            var seeded = await SeedGrantedSmsGuestAsync(GuestFrom);

            var form = BuildForm(GuestFrom, "+447700900000", "STOP");
            var signature = Sign(form);

            var status = await _sut.HandleAsync(RequestUrl, form, signature);

            Assert.Equal(TwilioSmsInboundStatus.Accepted, status);
            var states = await _permissions.GetCurrentStatesAsync(
                seeded.LocationGuestId
            );
            Assert.Equal(
                LocationGuestPermissionState.Granted,
                states[LocationGuestPermissionKind.SmsMarketing]
            );
        }

        [Fact]
        public async Task Handle_BadSignature_ReturnsForbidden()
        {
            var form = BuildForm(GuestFrom, MappedTo, "STOP");

            var status = await _sut.HandleAsync(
                RequestUrl,
                form,
                "bad-signature"
            );

            Assert.Equal(TwilioSmsInboundStatus.Forbidden, status);
        }

        private static Dictionary<string, string> BuildForm(
            string from,
            string to,
            string body
        ) =>
            new()
            {
                ["From"] = from,
                ["To"] = to,
                ["Body"] = body,
            };

        private static string Sign(Dictionary<string, string> form) =>
            TwilioSmsInboundEndpointsTests.ComputeTwilioSignature(
                AuthToken,
                RequestUrl,
                form
            );

        private async Task<SeededGuest> SeedGrantedSmsGuestAsync(string phoneE164)
        {
            var restaurant = new Restaurant
            {
                Name = "Svc Venue",
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
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

            // Production Guest Form stores digits-only NormalizedPhone.
            var digitsOnly = new string(phoneE164.Where(char.IsDigit).ToArray());
            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = phoneE164,
                NormalizedPhone = digitsOnly,
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(masterGuest);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                MasterGuest = masterGuest,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();

            _permissions.RecordEvent(
                guest.Id,
                guest.RestaurantLocationId,
                LocationGuestPermissionKind.SmsMarketing,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc)
            );
            await _context.SaveChangesAsync();

            return new SeededGuest(restaurant.Id, guest.Id);
        }

        public void Dispose() => _context.Dispose();

        private sealed record SeededGuest(int RestaurantId, int LocationGuestId);
    }
}

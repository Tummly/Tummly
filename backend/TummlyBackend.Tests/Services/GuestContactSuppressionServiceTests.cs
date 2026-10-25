using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="IGuestContactSuppressionService"/> — stage / find
    /// hashed guest contact suppression rows (no SaveChanges).
    /// </summary>
    public class GuestContactSuppressionServiceTests : IDisposable
    {
        private const string ContactKey = "email:alex@example.com";
        private const string PlainContact = "alex@example.com";

        private readonly ApplicationDbContext _context;
        private readonly IGuestContactSuppressionService _sut;

        public GuestContactSuppressionServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var hasher = new GuestContactHasher(
                "tests-only-guest-suppression-key"
            );
            _sut = new GuestContactSuppressionService(_context, hasher);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task StageAsync_TwiceSameKey_OneLocalRow_KeepsLaterDate()
        {
            var earlier = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var later = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

            await _sut.StageAsync(
                restaurantId: 1,
                ContactKey,
                LocationGuestPermissionKind.EmailMarketing,
                earlier,
                GuestContactSuppressionSources.GuestDelete
            );
            await _sut.StageAsync(
                restaurantId: 1,
                ContactKey,
                LocationGuestPermissionKind.EmailMarketing,
                later,
                GuestContactSuppressionSources.SmsStop
            );

            var local = _context.GuestContactSuppressions.Local.ToList();
            Assert.Single(local);
            Assert.Equal(later, local[0].WithdrawnAtUtc);
            Assert.DoesNotContain("@", local[0].ContactHash);
            Assert.DoesNotContain("alex", local[0].ContactHash);
            Assert.DoesNotContain("example", local[0].ContactHash);
        }

        [Fact]
        public async Task StageAsync_DoesNotLowerExistingLaterDbDate()
        {
            var later = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var earlier = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var hasher = new GuestContactHasher(
                "tests-only-guest-suppression-key"
            );
            var hash = hasher.Hash(ContactKey);

            _context.GuestContactSuppressions.Add(
                new GuestContactSuppression
                {
                    RestaurantId = 1,
                    PermissionKind =
                        LocationGuestPermissionKind.EmailMarketing,
                    ContactHash = hash,
                    WithdrawnAtUtc = later,
                    Source = GuestContactSuppressionSources.GuestDelete,
                    CreatedAtUtc = later,
                }
            );
            await _context.SaveChangesAsync();

            await _sut.StageAsync(
                restaurantId: 1,
                ContactKey,
                LocationGuestPermissionKind.EmailMarketing,
                earlier,
                GuestContactSuppressionSources.EmailUnsubscribe
            );

            var row = Assert.Single(
                _context.GuestContactSuppressions.Local
            );
            Assert.Equal(later, row.WithdrawnAtUtc);
            Assert.Equal(
                GuestContactSuppressionSources.GuestDelete,
                row.Source
            );
        }

        [Fact]
        public async Task FindAsync_ReturnsOnlyThatRestaurantRows()
        {
            await _sut.StageAsync(
                restaurantId: 1,
                ContactKey,
                LocationGuestPermissionKind.EmailMarketing,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                GuestContactSuppressionSources.GuestDelete
            );
            await _sut.StageAsync(
                restaurantId: 1,
                ContactKey,
                LocationGuestPermissionKind.SmsMarketing,
                new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                GuestContactSuppressionSources.GuestDelete
            );
            await _sut.StageAsync(
                restaurantId: 2,
                ContactKey,
                LocationGuestPermissionKind.EmailMarketing,
                new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                GuestContactSuppressionSources.GuestDelete
            );
            await _context.SaveChangesAsync();

            var found = await _sut.FindAsync(1, ContactKey);

            Assert.Equal(2, found.Count);
            Assert.All(found, row => Assert.Equal(1, row.RestaurantId));
            Assert.All(
                found,
                row =>
                {
                    Assert.DoesNotContain("@", row.ContactHash);
                    Assert.DoesNotContain(
                        PlainContact.Replace("@", string.Empty),
                        row.ContactHash
                    );
                }
            );
        }

        [Fact]
        public async Task StageAsync_StoredHash_HasNoPlainContact()
        {
            const string phoneKey = "phone:+447123456789";

            await _sut.StageAsync(
                restaurantId: 9,
                phoneKey,
                LocationGuestPermissionKind.SmsMarketing,
                new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                GuestContactSuppressionSources.SmsStop
            );

            var row = Assert.Single(
                _context.GuestContactSuppressions.Local
            );
            Assert.DoesNotContain("@", row.ContactHash);
            Assert.DoesNotContain("447123456789", row.ContactHash);
            Assert.DoesNotContain("07123456789", row.ContactHash);
            Assert.Equal(64, row.ContactHash.Length);
        }

        [Fact]
        public async Task StageAsync_UnspecifiedWithdrawnAt_StoredAsThatWallTimeUtc()
        {
            // Unspecified is common from SQL datetime2 / new DateTime(...).
            // ToUniversalTime() would treat it as local and shift the instant
            // (e.g. Asia/Karachi UTC+5: 12:00 → 07:00 UTC).
            var unspecified = new DateTime(
                2026,
                6,
                15,
                12,
                0,
                0,
                DateTimeKind.Unspecified
            );
            var expectedUtc = new DateTime(
                2026,
                6,
                15,
                12,
                0,
                0,
                DateTimeKind.Utc
            );

            await _sut.StageAsync(
                restaurantId: 1,
                ContactKey,
                LocationGuestPermissionKind.EmailMarketing,
                unspecified,
                GuestContactSuppressionSources.GuestDelete
            );

            var row = Assert.Single(
                _context.GuestContactSuppressions.Local
            );
            Assert.Equal(DateTimeKind.Utc, row.WithdrawnAtUtc.Kind);
            Assert.Equal(expectedUtc, row.WithdrawnAtUtc);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Guests;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="IGuestMarketingPreferenceUpdateService"/> — preference
    /// persists even when the later note create fails.
    /// </summary>
    public class GuestMarketingPreferenceUpdateServiceTests
    {
        [Fact]
        public async Task UpdateAsync_NoteSaveFailure_LeavesPreferencePersisted()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var failingContext = new FailNoteSaveDbContext(options);
            var recorder = new LocationGuestActivityRecorder(failingContext);
            var notes = new GuestNotesService(failingContext, recorder);
            var service = new GuestMarketingPreferenceUpdateService(
                failingContext,
                recorder,
                notes
            );

            var seeded = await SeedGuestAsync(
                failingContext,
                LocationGuestMarketingPreference.Allowed
            );

            var outcome = await service.UpdateAsync(
                seeded.LocationGuestId,
                seeded.LocationId,
                seeded.ActorUserId,
                new PatchGuestMarketingPreferenceRequest
                {
                    Preference = "opted_out",
                    Note = "Why this changed",
                }
            );

            Assert.Equal(
                GuestMarketingPreferenceUpdateStatus.Updated,
                outcome.Status
            );
            Assert.True(outcome.Result!.PreferenceChanged);
            Assert.False(outcome.Result.NoteCreated);
            Assert.Equal("Could not save the note.", outcome.Result.NoteError);

            var guest = await failingContext.LocationGuests
                .AsNoTracking()
                .SingleAsync(lg => lg.Id == seeded.LocationGuestId);
            Assert.Equal(
                LocationGuestMarketingPreference.OptedOut,
                guest.MarketingPreference
            );
            Assert.Equal(0, await failingContext.LocationGuestNotes.CountAsync());
            Assert.Equal(
                1,
                await failingContext.LocationGuestActivityEvents.CountAsync(e =>
                    e.Kind == LocationGuestActivityKinds.MarketingPreferenceChanged
                )
            );
        }

        private static async Task<Seed> SeedGuestAsync(
            ApplicationDbContext context,
            LocationGuestMarketingPreference preference
        )
        {
            var user = new User
            {
                FullName = "Ada Operator",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Marketing Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"{Guid.NewGuid():N}@guest.example.com",
                NormalizedEmail = $"{Guid.NewGuid():N}@guest.example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Test Guest",
                MarketingPreference = preference,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            return new Seed(user.Id, location.Id, locationGuest.Id);
        }

        /// <summary>
        /// Fails SaveChanges when a Location Guest note is staged after
        /// preference already persisted.
        /// </summary>
        private sealed class FailNoteSaveDbContext : ApplicationDbContext
        {
            public FailNoteSaveDbContext(
                DbContextOptions<ApplicationDbContext> options
            ) : base(options)
            {
            }

            public override Task<int> SaveChangesAsync(
                CancellationToken cancellationToken = default
            )
            {
                var stagingNote = ChangeTracker
                    .Entries<LocationGuestNote>()
                    .Any(entry => entry.State == EntityState.Added);

                if (stagingNote)
                {
                    throw new DbUpdateException(
                        "Simulated note SaveChanges failure."
                    );
                }

                return base.SaveChangesAsync(cancellationToken);
            }
        }

        private sealed record Seed(
            int ActorUserId,
            int LocationId,
            int LocationGuestId
        );
    }
}

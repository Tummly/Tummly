using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: ApplicationDbContext Campaign → RestaurantLocation FK delete
    /// policy (ticket campaigns-audit/15). Location hard-delete must not
    /// cascade-wipe Campaign Drafts.
    /// </summary>
    public class CampaignLocationFkRestrictTests : IDisposable
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        private readonly ApplicationDbContext _context;

        public CampaignLocationFkRestrictTests()
        {
            _context = CreateContext();
        }

        [Fact]
        public void Campaign_RestaurantLocationFk_UsesRestrictDelete()
        {
            var entity = _context.Model.FindEntityType(typeof(Campaign));
            Assert.NotNull(entity);

            var fk = entity!
                .GetForeignKeys()
                .Single(key =>
                    key.Properties.Select(p => p.Name)
                        .SequenceEqual(new[] { "RestaurantLocationId" })
                );

            Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        }

        [Fact]
        public async Task RemovingLocation_WithTrackedCampaign_FailsAndKeepsDraft()
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"fk-restrict-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "FK Restrict Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High St",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = "draft",
                Name = "Keep me",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            var campaignId = campaign.Id;
            var locationId = location.Id;

            // Restrict throws when a tracked Campaign still references the
            // location (client-side). SQL Server also rejects the DELETE with
            // a FK constraint error when dependents are not loaded.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    _context.RestaurantLocations.Remove(location);
                    await _context.SaveChangesAsync();
                }
            );

            Assert.Contains(
                "RestaurantLocation",
                exception.Message,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Campaign",
                exception.Message,
                StringComparison.Ordinal
            );

            _context.ChangeTracker.Clear();

            Assert.True(
                await _context.Campaigns.AnyAsync(c => c.Id == campaignId)
            );
            Assert.True(
                await _context.RestaurantLocations.AnyAsync(
                    l => l.Id == locationId
                )
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}

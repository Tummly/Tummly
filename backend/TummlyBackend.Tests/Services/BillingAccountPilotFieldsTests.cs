using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public class BillingAccountPilotFieldsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public BillingAccountPilotFieldsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public async Task BillingAccount_NewPilotFields_DefaultToEmpty()
        {
            const int restaurantId = 4201;
            _context.BillingAccounts.Add(new BillingAccount { RestaurantId = restaurantId });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var row = await _context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(account => account.RestaurantId == restaurantId);

            Assert.Null(row.PilotStartedAtUtc);
            Assert.Equal(0, row.PilotExtensionCount);
            Assert.Equal(0, row.PilotExtensionDaysTotal);
            Assert.Null(row.LastPilotExtensionKey);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

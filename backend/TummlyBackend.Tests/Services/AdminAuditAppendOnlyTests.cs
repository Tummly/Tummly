using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public class AdminAuditAppendOnlyTests : IDisposable
    {
        private readonly ApplicationDbContext _db;

        public AdminAuditAppendOnlyTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
        }

        [Fact]
        public async Task AppendingAnAuditRow_Saves()
        {
            _db.AdminAuditEvents.Add(NewRow());

            await _db.SaveChangesAsync();

            Assert.Equal(1, await _db.AdminAuditEvents.CountAsync());
        }

        [Fact]
        public async Task ModifyingAnAuditRow_Throws_AndKeepsTheStoredValue()
        {
            var row = NewRow();
            _db.AdminAuditEvents.Add(row);
            await _db.SaveChangesAsync();

            row.DetailJson = "{\"days\":30}";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _db.SaveChangesAsync()
            );
            Assert.Contains("append-only", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ModifyingAnAuditRow_SyncSave_Throws()
        {
            var row = NewRow();
            _db.AdminAuditEvents.Add(row);
            _db.SaveChanges();

            row.Succeeded = false;

            Assert.Throws<InvalidOperationException>(() => _db.SaveChanges());
        }

        [Fact]
        public async Task DeletingAnAuditRow_Throws()
        {
            var row = NewRow();
            _db.AdminAuditEvents.Add(row);
            await _db.SaveChangesAsync();

            _db.AdminAuditEvents.Remove(row);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _db.SaveChangesAsync()
            );
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        private static AdminAuditEvent NewRow() =>
            new()
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc),
                Action = AdminAuditActions.OperatorExtendPilot,
                ActorIdentity = "admin@example.com",
                TargetType = AdminAuditTargetTypes.OperatorUser,
                TargetId = "7",
                RestaurantId = 1,
                DetailJson = "{\"days\":14,\"idempotencyKey\":\"key-1\"}",
                Succeeded = true,
            };
    }
}

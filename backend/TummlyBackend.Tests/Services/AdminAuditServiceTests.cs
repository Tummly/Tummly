using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AdminAuditServiceTests
    {
        [Fact]
        public async Task Append_ThenList_ReturnsNewestFirst_RespectsTakeCap()
        {
            var clock = new FakeTimeProvider(
                new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero)
            );
            var svc = CreateService(out var db, clock);

            svc.Append(new AdminAuditAppendRequest(
                AdminAuditActions.TrialApprove,
                "admin@tummly.com",
                AdminAuditTargetTypes.TrialRequest,
                "1",
                ActorAdminUserId: 9
            ));
            clock.Advance(TimeSpan.FromMinutes(1));
            svc.Append(new AdminAuditAppendRequest(
                AdminAuditActions.CreditAdjust,
                "admin@tummly.com",
                AdminAuditTargetTypes.Restaurant,
                "42",
                RestaurantId: 42,
                DetailJson: """{"qty":10}"""
            ));
            await db.SaveChangesAsync();

            var page = await svc.ListAsync(new AdminAuditListQuery(Take: 1));
            Assert.Equal(2, page.TotalCount);
            Assert.Single(page.Items);
            Assert.Equal(AdminAuditActions.CreditAdjust, page.Items[0].Action);
        }

        [Fact]
        public async Task List_FiltersByActionAndRestaurantId()
        {
            var clock = new FakeTimeProvider(
                new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero)
            );
            var svc = CreateService(out var db, clock);

            svc.Append(new AdminAuditAppendRequest(
                AdminAuditActions.TrialApprove,
                "admin@tummly.com",
                AdminAuditTargetTypes.TrialRequest,
                "1",
                ActorAdminUserId: 9
            ));
            clock.Advance(TimeSpan.FromMinutes(1));
            svc.Append(new AdminAuditAppendRequest(
                AdminAuditActions.CreditAdjust,
                "admin@tummly.com",
                AdminAuditTargetTypes.Restaurant,
                "42",
                RestaurantId: 42,
                DetailJson: """{"qty":10}"""
            ));
            await db.SaveChangesAsync();

            var page = await svc.ListAsync(
                new AdminAuditListQuery(
                    Action: AdminAuditActions.CreditAdjust,
                    RestaurantId: 42
                )
            );

            Assert.Equal(1, page.TotalCount);
            Assert.Single(page.Items);
            Assert.Equal(AdminAuditActions.CreditAdjust, page.Items[0].Action);
            Assert.Equal(42, page.Items[0].RestaurantId);
        }

        [Fact]
        public async Task List_ClampsTakeTo100()
        {
            var clock = new FakeTimeProvider(
                new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero)
            );
            var svc = CreateService(out var db, clock);

            for (var i = 0; i < 101; i++)
            {
                clock.Advance(TimeSpan.FromSeconds(1));
                svc.Append(new AdminAuditAppendRequest(
                    AdminAuditActions.TrialApprove,
                    "admin@tummly.com",
                    AdminAuditTargetTypes.TrialRequest,
                    i.ToString()
                ));
            }

            await db.SaveChangesAsync();

            var page = await svc.ListAsync(new AdminAuditListQuery(Take: 500));
            Assert.Equal(101, page.TotalCount);
            Assert.Equal(100, page.Items.Count);
        }

        private static IAdminAuditService CreateService(
            out ApplicationDbContext db,
            TimeProvider clock
        )
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            db = new ApplicationDbContext(options);
            return new AdminAuditService(db, clock);
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private DateTimeOffset _utcNow;

            public FakeTimeProvider(DateTimeOffset utcNow)
            {
                _utcNow = utcNow;
            }

            public void Advance(TimeSpan delta) => _utcNow += delta;

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }
    }
}

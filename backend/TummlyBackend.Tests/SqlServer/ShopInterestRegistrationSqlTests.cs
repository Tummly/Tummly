using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Migrations;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.SqlServer
{
    /// <summary>
    /// Filtered unique index on active Register interest rows, the race fallback
    /// in <see cref="ShopInterestService.RegisterAsync"/>, and the Lane 4 data steps.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public sealed class ShopInterestRegistrationSqlTests
    {
        private const int RestaurantId = 3;
        private const int LocationId = 7;
        private const int UserId = 41;

        private readonly SqlServerFixture _fixture;

        public ShopInterestRegistrationSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task UniqueIndex_RejectsSecondActiveRow_ForTheSameKey()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var productId = await SeedProductAsync();

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();
            context.ShopInterestRegistrations.Add(Row(productId));
            await context.SaveChangesAsync();
            context.ShopInterestRegistrations.Add(Row(productId));

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync()
            );
            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task UniqueIndex_AllowsANewActiveRow_AfterTheOldOneIsWithdrawn()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var productId = await SeedProductAsync();

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();
            var first = Row(productId);
            context.ShopInterestRegistrations.Add(first);
            await context.SaveChangesAsync();
            first.WithdrawnAtUtc = DateTime.UtcNow;
            first.WithdrawnByUserId = UserId;
            await context.SaveChangesAsync();

            context.ShopInterestRegistrations.Add(Row(productId));
            await context.SaveChangesAsync();

            Assert.Equal(
                2,
                await context.ShopInterestRegistrations.CountAsync(row =>
                    row.ShopInterestProductId == productId
                )
            );
            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task Register_RaceLoss_ReturnsTheExistingActiveRowAsSuccess()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var productKey = $"lane4-race-{Guid.NewGuid():N}";
            var productId = await SeedProductAsync(productKey);

            // The rival insert lands after the existence check and before our insert,
            // so the filtered unique index rejects ours.
            var rival = new RivalRegistrationInterceptor(
                _fixture.ConnectionString,
                productId
            );
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_fixture.ConnectionString)
                .AddInterceptors(rival)
                .Options;
            await using var context = new ApplicationDbContext(options);
            var service = new ShopInterestService(context);

            var (ok, error, message) = await service.RegisterAsync(
                UserId,
                RestaurantId,
                "Interest Venue",
                new RegisterShopInterestRequest
                {
                    ProductKey = productKey,
                    LocationId = LocationId,
                },
                "Main Dining",
                "Jane Doe",
                "jane.doe@example.com"
            );

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(ShopInterestService.ThanksMessage, message);
            // Without the rival insert the test would pass for the wrong reason.
            Assert.True(rival.RivalId.HasValue);
            await using var verify = _fixture.CreateContext();
            var active = await verify.ShopInterestRegistrations
                .Where(row =>
                    row.ShopInterestProductId == productId
                    && row.WithdrawnAtUtc == null
                )
                .ToListAsync();
            Assert.Equal(rival.RivalId, Assert.Single(active).Id);
        }

        [SkippableFact]
        public async Task Migration_DedupeKeepsOldestActiveRow_AndWithdrawsTheRest()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var productId = await SeedProductAsync();

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            // Recreate the pre-migration state: no unique index, duplicate active rows.
            await context.Database.ExecuteSqlRawAsync(
                "DROP INDEX [IX_ShopInterestRegistrations_ShopInterestProductId_RestaurantId_LocationId_UserId] ON [ShopInterestRegistrations]"
            );
            var oldest = Row(productId, createdAtUtc: new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc));
            var newer = Row(productId, createdAtUtc: new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc));
            var nullLocation = Row(productId, createdAtUtc: new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc));
            nullLocation.LocationId = null;
            var nullLocationDuplicate = Row(productId, createdAtUtc: new DateTime(2026, 10, 4, 9, 0, 0, DateTimeKind.Utc));
            nullLocationDuplicate.LocationId = null;
            context.ShopInterestRegistrations.AddRange(oldest, newer, nullLocation, nullLocationDuplicate);
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync(
                Lane4_ShopInterestWithdrawal.DedupeActiveRegistrationsSql
            );

            context.ChangeTracker.Clear();
            var rows = await context.ShopInterestRegistrations
                .AsNoTracking()
                .Where(row => row.ShopInterestProductId == productId)
                .ToDictionaryAsync(row => row.Id);
            Assert.Null(rows[oldest.Id].WithdrawnAtUtc);
            Assert.NotNull(rows[newer.Id].WithdrawnAtUtc);
            Assert.Null(rows[newer.Id].WithdrawnByUserId);
            Assert.Null(rows[nullLocation.Id].WithdrawnAtUtc);
            Assert.NotNull(rows[nullLocationDuplicate.Id].WithdrawnAtUtc);
            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task Migration_CorrectsOldConceptName_OnlyWhileTheRowStillHoldsIt()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();
            // The shared database may already hold the seeded concepts: reuse those rows
            // (ProductKey is unique) and give them the titles this test needs.
            var product = await UpsertConceptAsync(
                context,
                "standard-package-label",
                "Standard Package Label"
            );
            var edited = await UpsertConceptAsync(context, "drink-cup-seal", "Admin edited title");
            await context.SaveChangesAsync();
            var registration = Row(product.Id);
            registration.ProductKey = "standard-package-label";
            registration.ProductTitleSnapshot = "Standard Package Label";
            context.ShopInterestRegistrations.Add(registration);
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync(
                Lane4_ShopInterestWithdrawal.CorrectConceptTitlesSql
            );

            context.ChangeTracker.Clear();
            Assert.Equal(
                "Standard Packaging Label",
                (await context.ShopInterestRegistrations.AsNoTracking()
                    .SingleAsync(row => row.Id == registration.Id)).ProductTitleSnapshot
            );
            Assert.Equal(
                "Standard Packaging Label",
                (await context.ShopInterestProducts.AsNoTracking()
                    .SingleAsync(row => row.Id == product.Id)).Title
            );
            Assert.Equal(
                "Admin edited title",
                (await context.ShopInterestProducts.AsNoTracking()
                    .SingleAsync(row => row.Id == edited.Id)).Title
            );
            await tx.RollbackAsync();
        }

        private async Task<int> SeedProductAsync(string? productKey = null)
        {
            await using var context = _fixture.CreateContext();
            var product = new ShopInterestProduct
            {
                ProductKey = productKey ?? $"lane4-{Guid.NewGuid():N}",
                Title = "Tray Liner",
                Description = "Concept",
                Material = string.Empty,
                Dimensions = string.Empty,
                IsActive = true,
            };
            context.ShopInterestProducts.Add(product);
            await context.SaveChangesAsync();
            return product.Id;
        }

        private static async Task<ShopInterestProduct> UpsertConceptAsync(
            ApplicationDbContext context,
            string productKey,
            string title
        )
        {
            var row = await context.ShopInterestProducts.SingleOrDefaultAsync(
                product => product.ProductKey == productKey
            );
            if (row == null)
            {
                row = new ShopInterestProduct
                {
                    ProductKey = productKey,
                    Description = "Concept",
                    Material = string.Empty,
                    Dimensions = string.Empty,
                    IsActive = true,
                };
                context.ShopInterestProducts.Add(row);
            }

            row.Title = title;
            return row;
        }

        private static ShopInterestRegistration Row(
            int productId,
            DateTime? createdAtUtc = null
        ) =>
            new()
            {
                ShopInterestProductId = productId,
                ProductKey = "lane4",
                ProductTitleSnapshot = "Tray Liner",
                RestaurantId = RestaurantId,
                RestaurantNameSnapshot = "Interest Venue",
                LocationId = LocationId,
                LocationNameSnapshot = "Main Dining",
                UserId = UserId,
                UserFullNameSnapshot = "Jane Doe",
                UserEmailSnapshot = "jane.doe@example.com",
                CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            };

        /// <summary>
        /// Inserts the rival registration on a second context just before our save runs.
        /// </summary>
        private sealed class RivalRegistrationInterceptor : SaveChangesInterceptor
        {
            private readonly string _connectionString;
            private readonly int _productId;
            private bool _fired;

            public int? RivalId { get; private set; }

            public RivalRegistrationInterceptor(string connectionString, int productId)
            {
                _connectionString = connectionString;
                _productId = productId;
            }

            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default
            )
            {
                if (!_fired)
                {
                    _fired = true;
                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer(_connectionString)
                        .Options;
                    await using var rival = new ApplicationDbContext(options);
                    var row = Row(_productId);
                    rival.ShopInterestRegistrations.Add(row);
                    await rival.SaveChangesAsync(cancellationToken);
                    RivalId = row.Id;
                }

                return result;
            }
        }
    }
}

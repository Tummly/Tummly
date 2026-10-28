using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.SqlServer
{
    /// <summary>
    /// The premises review on SQL Server: the migration columns, the indexed
    /// key lookup, the legacy-row fallback and the Starter Kit address
    /// snapshot all have to translate to T-SQL, which InMemory cannot show.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public sealed class PremisesReviewSqlTests
    {
        private readonly SqlServerFixture _fixture;

        public PremisesReviewSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task Migration_PersistsReviewColumns_AndIndexedKeyLookupMatches()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var postcode = $"ZZ{suffix[..2].ToUpperInvariant()} 1AA";
            var theirs = await SeedWorkspaceAsync("theirs");
            var address = $"{suffix} Review Road";
            var key = PremisesKey.Build(address, "Leeds", postcode);

            await using (var write = _fixture.CreateContext())
            {
                write.RestaurantLocations.Add(
                    new RestaurantLocation
                    {
                        RestaurantId = theirs.RestaurantId,
                        LocationName = "Theirs",
                        Address = address,
                        City = "Leeds",
                        Postcode = postcode,
                        PremisesKey = key,
                        FsaEstablishmentId = $"FSA{suffix}",
                        PremisesReviewStatus = PremisesReviewStatus.Cleared,
                        PremisesReviewedAtUtc = DateTime.UtcNow,
                        PremisesReviewedByAdminUserId = 7,
                        PremisesReviewReason = "Checked.",
                        LifecycleStatus = LocationLifecycleStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await write.SaveChangesAsync();
            }

            await using var context = _fixture.CreateContext();
            var service = new PremisesReviewService(context);
            var ours = await SeedWorkspaceAsync("ours");

            var byKey = await service.CheckAsync(
                new PremisesCheckInput(ours.RestaurantId, address.ToLowerInvariant(), "Leeds", postcode, null)
            );
            var byFsa = await service.CheckAsync(
                new PremisesCheckInput(ours.RestaurantId, "1 Elsewhere Lane", "York", "YO1 7HH", $"fsa{suffix}")
            );
            var unrelated = await service.CheckAsync(
                new PremisesCheckInput(ours.RestaurantId, "9 Nowhere Lane", "York", "YO1 7HH", null)
            );

            Assert.Single(byKey.Matches);
            Assert.True(byKey.Matches[0].PremisesKeyMatch);
            Assert.False(byKey.Matches[0].SameWorkspace);
            Assert.Single(byFsa.Matches);
            Assert.True(byFsa.Matches[0].FsaMatch);
            Assert.False(unrelated.Flagged);
        }

        [SkippableFact]
        public async Task LegacyRowWithoutKey_IsMatched_AndBackfilledOnSave()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var postcode = $"ZY{suffix[..2].ToUpperInvariant()} 2BB";
            var address = $"{suffix} Legacy Road";
            var theirs = await SeedWorkspaceAsync("legacy-theirs");
            int legacyId;
            await using (var write = _fixture.CreateContext())
            {
                var legacy = new RestaurantLocation
                {
                    RestaurantId = theirs.RestaurantId,
                    LocationName = "Legacy",
                    Address = address,
                    City = "Leeds",
                    Postcode = postcode,
                    LifecycleStatus = LocationLifecycleStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                };
                write.RestaurantLocations.Add(legacy);
                await write.SaveChangesAsync();
                legacyId = legacy.Id;
            }

            var ours = await SeedWorkspaceAsync("legacy-ours");
            await using (var context = _fixture.CreateContext())
            {
                var result = await new PremisesReviewService(context).CheckAsync(
                    new PremisesCheckInput(ours.RestaurantId, address, "Leeds", postcode.ToLowerInvariant(), null)
                );
                Assert.Single(result.Matches);
                await context.SaveChangesAsync();
            }

            await using var verify = _fixture.CreateContext();
            var key = await verify.RestaurantLocations
                .Where(row => row.Id == legacyId)
                .Select(row => row.PremisesKey)
                .SingleAsync();
            Assert.Equal(PremisesKey.Build(address, "Leeds", postcode), key);
        }

        [SkippableFact]
        public async Task StarterKitAddressSnapshot_FlagsEvenAfterTheLocationMoved()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var postcode = $"ZX{suffix[..2].ToUpperInvariant()} 3CC";
            var address = $"{suffix} Kit Road";
            var kitHolder = await SeedWorkspaceAsync("kit", withBilling: true);
            int locationId;
            await using (var write = _fixture.CreateContext())
            {
                await ShopMaterialsCatalogSeed.EnsureSeededAsync(write);
                var location = new RestaurantLocation
                {
                    RestaurantId = kitHolder.RestaurantId,
                    LocationName = "Kit Site",
                    Address = address,
                    City = "Leeds",
                    Postcode = postcode,
                    LocalContact = "Contact",
                    LifecycleStatus = LocationLifecycleStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                };
                write.RestaurantLocations.Add(location);
                await write.SaveChangesAsync();
                locationId = location.Id;

                var order = await new ComplimentaryStarterShopOrderService(
                    write,
                    new DbMaterialsCatalog(write),
                    new ShopOrderNumberAllocator(write)
                ).EnsureForLocationAsync(kitHolder.RestaurantId, locationId, kitHolder.UserId, "Owner");
                Assert.NotEqual(Guid.Empty, order.ShopOrderId);

                location.Address = "Moved Away Road";
                location.Postcode = "ZW1 1AA";
                await write.SaveChangesAsync();
            }

            var ours = await SeedWorkspaceAsync("kit-ours");
            await using var context = _fixture.CreateContext();
            var result = await new PremisesReviewService(context).CheckAsync(
                new PremisesCheckInput(ours.RestaurantId, address, "Leeds", postcode, null)
            );

            var match = Assert.Single(result.Matches);
            Assert.Equal(locationId, match.LocationId);
            Assert.True(match.PriorStarterKit);
        }

        [SkippableFact]
        public async Task Gate_FindsBlockedLocationsByWorkspace_OnSqlServer()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            var workspace = await SeedWorkspaceAsync("gate");
            await using var context = _fixture.CreateContext();
            Assert.False(
                await PremisesReviewGate.WorkspaceHasBlockedLocationAsync(context, workspace.RestaurantId)
            );

            var location = new RestaurantLocation
            {
                RestaurantId = workspace.RestaurantId,
                LocationName = "Held",
                Address = "1 Gate Road",
                City = "Leeds",
                Postcode = "LS1 4GG",
                PremisesReviewStatus = PremisesReviewStatus.PendingReview,
                LifecycleStatus = LocationLifecycleStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            Assert.True(
                await PremisesReviewGate.WorkspaceHasBlockedLocationAsync(context, workspace.RestaurantId)
            );
            Assert.True(await PremisesReviewGate.LocationIsBlockedAsync(context, location.Id));
        }

        private async Task<(int RestaurantId, int UserId)> SeedWorkspaceAsync(
            string label,
            bool withBilling = false
        )
        {
            await using var context = _fixture.CreateContext();
            var suffix = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;
            var owner = new User
            {
                FullName = $"Owner {label}",
                Email = $"premises-{label}-{suffix}@example.com",
                PasswordHash = "hash",
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = now,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"Premises {label} {suffix}",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            if (withBilling)
            {
                context.ShopOrderSequences.Add(
                    new ShopOrderSequence { RestaurantId = restaurant.Id, NextNumber = 1 }
                );
                context.BillingAccounts.Add(
                    new BillingAccount
                    {
                        RestaurantId = restaurant.Id,
                        RevolutCustomerId = $"cust_premises_{suffix}",
                        SubscriptionPlan = BillingSubscriptionPlans.Starter,
                        BillingCycle = BillingCycles.Monthly,
                        BillingStatus = BillingStatuses.Active,
                        ContractedPricebookId = ShopCapacityTestSeed.PricebookId,
                    }
                );
                await context.SaveChangesAsync();
            }

            return (restaurant.Id, owner.Id);
        }
    }
}

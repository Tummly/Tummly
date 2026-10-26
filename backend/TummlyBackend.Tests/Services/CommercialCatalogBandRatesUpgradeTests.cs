using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class CommercialCatalogBandRatesUpgradeTests
    {
        private const string UpgradePublicId = "TUMMLY-UK-GBP-BANDS-20261026";

        private static readonly string[] BandKeys =
        [
            CommercialCustomUnitRateKeys.ExtraLocationBand2,
            CommercialCustomUnitRateKeys.ExtraLocationBand3,
            CommercialCustomUnitRateKeys.ExtraLocationBand4,
            CommercialCustomUnitRateKeys.ExtraLocationBand5,
        ];

        private static ApplicationDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// A Published version as an existing database holds it: seeded rows without band rates.
        /// </summary>
        private static CommercialCatalogVersion PublishedWithoutBands()
        {
            var json = PricebookCatalog.LoadFromDirectory(
                TestPricebookPaths.PackDirectory()
            );
            var snapshot = json.GetRequired(json.CurrentPricebookId);
            var version = CommercialCatalogSeeder.FromSnapshot(snapshot, DateTime.UtcNow);
            version.CustomUnitRates = version
                .CustomUnitRates.Where(r => !BandKeys.Contains(r.RateKey))
                .ToList();
            return version;
        }

        private static async Task<List<CommercialCatalogVersion>> LoadAllAsync(
            ApplicationDbContext db
        )
        {
            return await db
                .CommercialCatalogVersions.Include(v => v.Plans)
                .Include(v => v.TopUpPacks)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates)
                .AsNoTracking()
                .ToListAsync();
        }

        private static object[] PlanShape(CommercialCatalogVersion version) =>
            version
                .Plans.OrderBy(p => p.PlanKey, StringComparer.Ordinal)
                .Select(p =>
                    (object)
                        new
                        {
                            p.PlanKey,
                            p.DisplayName,
                            p.MonthlyNetPence,
                            p.AnnualNetPence,
                            p.IncludedLocations,
                            p.IncludedTeamMembers,
                            p.ActiveOffersAccount,
                            p.ActiveQrPlacementsPerLocation,
                            p.PublishedGuestFormsPerLocation,
                            p.DraftGuestFormsPerLocation,
                            p.CreditsMonthlyAi,
                            p.CreditsMonthlyEmail,
                            p.CreditsMonthlySms,
                            p.CreditsOneTimeAi,
                            p.CreditsOneTimeEmail,
                            p.CreditsOneTimeSms,
                            p.LookupKeyMonthly,
                            p.LookupKeyAnnual,
                            p.RevolutPlanVariationIdMonthly,
                            p.RevolutPlanVariationIdAnnual,
                            p.SelfServeMaxLocations,
                        }
                )
                .ToArray();

        private static object[] PackShape(CommercialCatalogVersion version) =>
            version
                .TopUpPacks.OrderBy(p => p.LookupKey, StringComparer.Ordinal)
                .Select(p =>
                    (object)
                        new
                        {
                            p.Channel,
                            p.Quantity,
                            p.NetPence,
                            p.LookupKey,
                            p.ApprovalRequired,
                        }
                )
                .ToArray();

        private static object[] ExtraShape(CommercialCatalogVersion version) =>
            version
                .ExtraLocations.Select(e =>
                    (object)
                        new
                        {
                            e.MonthlyNetPence,
                            e.AnnualNetPence,
                            e.UsersAdded,
                            e.CreditsMonthlyAi,
                            e.CreditsMonthlyEmail,
                            e.CreditsMonthlySms,
                            e.LookupKeyMonthly,
                            e.LookupKeyAnnual,
                            e.RevolutPlanVariationIdMonthly,
                            e.RevolutPlanVariationIdAnnual,
                        }
                )
                .ToArray();

        private static object[] NonBandRateShape(CommercialCatalogVersion version) =>
            version
                .CustomUnitRates.Where(r => !BandKeys.Contains(r.RateKey))
                .OrderBy(r => r.RateKey, StringComparer.Ordinal)
                .Select(r =>
                    (object)
                        new
                        {
                            r.RateKey,
                            r.UnitQuantity,
                            r.MonthlyNetPence,
                            r.AnnualNetPence,
                        }
                )
                .ToArray();

        [Fact]
        public async Task EnsureAsync_PublishedWithoutBands_PublishesNewVersion()
        {
            using var db = NewContext();
            var old = PublishedWithoutBands();
            db.CommercialCatalogVersions.Add(old);
            await db.SaveChangesAsync();

            var published = await CommercialCatalogBandRatesUpgrade.EnsureAsync(
                db,
                DateTime.UtcNow,
                NullLogger.Instance
            );

            Assert.True(published);
            var all = await LoadAllAsync(db);
            Assert.Equal(2, all.Count);

            var prior = all.Single(v => v.Id == old.Id);
            Assert.Equal(CommercialCatalogStatuses.Superseded, prior.Status);

            var current = all.Single(v => v.Id != old.Id);
            Assert.Equal(CommercialCatalogStatuses.Published, current.Status);
            Assert.Single(all, v => v.Status == CommercialCatalogStatuses.Published);
            Assert.Equal(UpgradePublicId, current.PublicId);
            Assert.Equal("Location bands (provisional, doc 08 R3)", current.Notes);

            foreach (var band in LocationBandPricing.Bands.Where(b => b.Index > 1))
            {
                var rows = current.CustomUnitRates.Where(r => r.RateKey == band.RateKey).ToList();
                var expected = LocationBandPricing.ProvisionalRates[band.Index];
                var row = Assert.Single(rows);
                Assert.Equal(1, row.UnitQuantity);
                Assert.Equal(expected.MonthlyNetPence, row.MonthlyNetPence);
                Assert.Equal(expected.AnnualNetPence, row.AnnualNetPence);
            }
            Assert.Equal(4, current.CustomUnitRates.Count(r => BandKeys.Contains(r.RateKey)));

            var oldGroup = prior.Plans.Single(p => p.PlanKey == "group");
            var newGroup = current.Plans.Single(p => p.PlanKey == "group");
            Assert.Equal(30, newGroup.SelfServeMaxLocations);
            Assert.Equal(oldGroup.SelfServeMaxLocations, newGroup.SelfServeMaxLocations);

            Assert.Equal(PlanShape(prior), PlanShape(current));
            Assert.Equal(PackShape(prior), PackShape(current));
            Assert.Equal(ExtraShape(prior), ExtraShape(current));
            Assert.Equal(prior.VatRateBps, current.VatRateBps);
            Assert.Equal(NonBandRateShape(prior), NonBandRateShape(current));
        }

        [Fact]
        public async Task EnsureAsync_Idempotent()
        {
            using var db = NewContext();
            db.CommercialCatalogVersions.Add(PublishedWithoutBands());
            await db.SaveChangesAsync();

            var first = await CommercialCatalogBandRatesUpgrade.EnsureAsync(
                db,
                DateTime.UtcNow,
                NullLogger.Instance
            );
            var countAfterFirst = await db.CommercialCatalogVersions.CountAsync();

            var second = await CommercialCatalogBandRatesUpgrade.EnsureAsync(
                db,
                DateTime.UtcNow,
                NullLogger.Instance
            );

            Assert.True(first);
            Assert.False(second);
            Assert.Equal(countAfterFirst, await db.CommercialCatalogVersions.CountAsync());
        }

        [Fact]
        public async Task EnsureAsync_AlreadyBanded_ReturnsFalseWithoutNewVersion()
        {
            using var db = NewContext();
            var json = PricebookCatalog.LoadFromDirectory(
                TestPricebookPaths.PackDirectory()
            );
            var snapshot = json.GetRequired(json.CurrentPricebookId);
            db.CommercialCatalogVersions.Add(
                CommercialCatalogSeeder.FromSnapshot(snapshot, DateTime.UtcNow)
            );
            await db.SaveChangesAsync();

            var published = await CommercialCatalogBandRatesUpgrade.EnsureAsync(
                db,
                DateTime.UtcNow,
                NullLogger.Instance
            );

            Assert.False(published);
            Assert.Equal(1, await db.CommercialCatalogVersions.CountAsync());
        }

        [Fact]
        public async Task EnsureAsync_KeepsAdminEditedLocationRate()
        {
            using var db = NewContext();
            var old = PublishedWithoutBands();
            var location = old.CustomUnitRates.Single(r =>
                r.RateKey == CommercialCustomUnitRateKeys.Location
            );
            location.MonthlyNetPence = 4100;
            db.CommercialCatalogVersions.Add(old);
            await db.SaveChangesAsync();

            await CommercialCatalogBandRatesUpgrade.EnsureAsync(
                db,
                DateTime.UtcNow,
                NullLogger.Instance
            );

            var current = (await LoadAllAsync(db)).Single(v =>
                v.Status == CommercialCatalogStatuses.Published
            );
            var newLocation = current.CustomUnitRates.Single(r =>
                r.RateKey == CommercialCustomUnitRateKeys.Location
            );
            Assert.Equal(4100, newLocation.MonthlyNetPence);
        }

        [Fact]
        public async Task EnsureAsync_LeavesDraftAlone()
        {
            using var db = NewContext();
            var old = PublishedWithoutBands();
            db.CommercialCatalogVersions.Add(old);
            await db.SaveChangesAsync();

            var draft = CommercialCatalogMapper.CloneAsDraft(old, DateTime.UtcNow);
            db.CommercialCatalogVersions.Add(draft);
            await db.SaveChangesAsync();
            var draftRateCount = draft.CustomUnitRates.Count;
            var draftPlanCount = draft.Plans.Count;

            await CommercialCatalogBandRatesUpgrade.EnsureAsync(
                db,
                DateTime.UtcNow,
                NullLogger.Instance
            );

            var reloaded = (await LoadAllAsync(db)).Single(v => v.Id == draft.Id);
            Assert.Equal(CommercialCatalogStatuses.Draft, reloaded.Status);
            Assert.Equal(draftRateCount, reloaded.CustomUnitRates.Count);
            Assert.Equal(draftPlanCount, reloaded.Plans.Count);
            Assert.DoesNotContain(
                reloaded.CustomUnitRates,
                r => BandKeys.Contains(r.RateKey)
            );
        }
        [Fact]
        public async Task TryEnsureInOwnScopeAsync_WhenUpgradeFails_ReturnsFalseAndLaterScopeStillPublishes()
        {
            var dbName = Guid.NewGuid().ToString();
            var provider = new ServiceCollection()
                .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName))
                .BuildServiceProvider();
            using (var seedScope = provider.CreateScope())
            {
                var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                seedDb.CommercialCatalogVersions.Add(PublishedWithoutBands());
                await seedDb.SaveChangesAsync();
            }

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();

            var failed = await CommercialCatalogBandRatesUpgrade.TryEnsureInOwnScopeAsync(
                provider,
                NullLogger.Instance,
                cancelled.Token
            );

            Assert.False(failed);
            using (var checkScope = provider.CreateScope())
            {
                var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Equal(1, await checkDb.CommercialCatalogVersions.CountAsync());
            }

            var published = await CommercialCatalogBandRatesUpgrade.TryEnsureInOwnScopeAsync(
                provider,
                NullLogger.Instance
            );

            Assert.True(published);
            using var finalScope = provider.CreateScope();
            var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(2, await finalDb.CommercialCatalogVersions.CountAsync());
        }

        [Fact]
        public async Task TryEnsureInOwnScopeAsync_WhenContextNotRegistered_ReturnsFalse()
        {
            var provider = new ServiceCollection().BuildServiceProvider();

            var published = await CommercialCatalogBandRatesUpgrade.TryEnsureInOwnScopeAsync(
                provider,
                NullLogger.Instance
            );

            Assert.False(published);
        }
    }
}

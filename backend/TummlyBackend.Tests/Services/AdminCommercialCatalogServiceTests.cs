using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class AdminCommercialCatalogServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AdminCommercialCatalogService _sut;
        private readonly DbPricebookCatalog _dbCatalog;

        public AdminCommercialCatalogServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            var json = PricebookCatalog.LoadFromDirectory(
                TestPricebookPaths.PackDirectory()
            );
            var snapshot = json.GetRequired(json.CurrentPricebookId);
            var seeded = CommercialCatalogSeeder.FromSnapshot(
                snapshot,
                DateTime.UtcNow
            );
            _context.CommercialCatalogVersions.Add(seeded);
            _context.SaveChanges();

            var scopeFactory = new TestScopeFactory(_context);
            var env = new TestHostEnvironment();
            _dbCatalog = new DbPricebookCatalog(scopeFactory, env);
            _sut = new AdminCommercialCatalogService(
                _context,
                new AdminAuditService(_context, TimeProvider.System),
                _dbCatalog
            );
        }

        [Fact]
        public async Task SeedParity_MatchesV3StarterMonthly()
        {
            var published = await _sut.GetPublishedAsync();
            Assert.NotNull(published);
            var starter = published!.Plans.First(p => p.PlanKey == "starter");
            Assert.Equal(3900, starter.MonthlyNetPence);
            Assert.Equal(100, starter.CreditsMonthlySms);
            Assert.Contains(
                published.CustomUnitRates,
                r => r.RateKey == CommercialCustomUnitRateKeys.Location
            );
        }

        [Fact]
        public async Task Publish_SwitchesCurrent_AndGrandfathersPriorPublicId()
        {
            var priorPublicId = (await _sut.GetPublishedAsync())!.PublicId;
            await _sut.EnsureDraftAsync(1);
            var (draft, _) = await _sut.SaveDraftAsync(
                1,
                new AdminCommercialCatalogSaveDraftRequest
                {
                    Plans =
                    [
                        new AdminCommercialPlanDto
                        {
                            PlanKey = "starter",
                            DisplayName = "Starter",
                            MonthlyNetPence = 4900,
                            AnnualNetPence = 50000,
                            IncludedLocations = 1,
                            IncludedTeamMembers = 3,
                            ActiveOffersAccount = 3,
                            ActiveQrPlacementsPerLocation = 10,
                            PublishedGuestFormsPerLocation = 1,
                            DraftGuestFormsPerLocation = 1,
                            CreditsMonthlyAi = 100,
                            CreditsMonthlyEmail = 2500,
                            CreditsMonthlySms = 100,
                        },
                    ],
                }
            );
            Assert.NotNull(draft);

            var (published, error) = await _sut.PublishAsync(
                1,
                new AdminCommercialCatalogPublishRequest
                {
                    PublicId = "TUMMLY-UK-GBP-TEST-PUBLISH",
                }
            );
            Assert.Null(error);
            Assert.NotNull(published);
            Assert.Equal("TUMMLY-UK-GBP-TEST-PUBLISH", published!.PublicId);

            _dbCatalog.InvalidateCache();
            Assert.Equal(
                "TUMMLY-UK-GBP-TEST-PUBLISH",
                _dbCatalog.CurrentPricebookId
            );
            Assert.Equal(
                4900,
                _dbCatalog.GetRequired(_dbCatalog.CurrentPricebookId)
                    .Plans["starter"]
                    .MonthlyNetPence
            );

            // Prior public id still resolvable for grandfathered accounts.
            var prior = _dbCatalog.GetRequired(priorPublicId);
            Assert.Equal(3900, prior.Plans["starter"].MonthlyNetPence);
        }

        [Fact]
        public async Task TopUpPackPriceChange_FlowsToSnapshot()
        {
            await _sut.EnsureDraftAsync(1);
            var draft = await _sut.GetDraftAsync();
            var packs = draft!.TopUpPacks
                .Select(p =>
                {
                    if (p.Channel == "sms" && p.Quantity == 100)
                    {
                        p.NetPence = 1500;
                    }

                    return p;
                })
                .ToList();

            await _sut.SaveDraftAsync(
                1,
                new AdminCommercialCatalogSaveDraftRequest { TopUpPacks = packs }
            );
            await _sut.PublishAsync(
                1,
                new AdminCommercialCatalogPublishRequest
                {
                    PublicId = "TUMMLY-UK-GBP-TEST-TOPUP",
                }
            );
            _dbCatalog.InvalidateCache();
            var sms100 = _dbCatalog
                .GetRequired(_dbCatalog.CurrentPricebookId)
                .TopUpPacks.First(p => p.Channel == "sms" && p.Quantity == 100);
            Assert.Equal(1500, sms100.NetPence);
        }

        private sealed class TestScopeFactory : IServiceScopeFactory
        {
            private readonly ApplicationDbContext _db;

            public TestScopeFactory(ApplicationDbContext db)
            {
                _db = db;
            }

            public IServiceScope CreateScope()
            {
                return new TestScope(_db);
            }

            private sealed class TestScope : IServiceScope
            {
                public TestScope(ApplicationDbContext db)
                {
                    ServiceProvider = new TestSp(db);
                }

                public IServiceProvider ServiceProvider { get; }

                public void Dispose()
                {
                }
            }

            private sealed class TestSp : IServiceProvider
            {
                private readonly ApplicationDbContext _db;

                public TestSp(ApplicationDbContext db)
                {
                    _db = db;
                }

                public object? GetService(Type serviceType)
                {
                    if (serviceType == typeof(ApplicationDbContext))
                    {
                        return _db;
                    }

                    return null;
                }
            }
        }

        private sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Development";

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } =
                Path.GetFullPath(
                    Path.Combine(
                        TestPricebookPaths.PackDirectory(),
                        "..",
                        "..",
                        "..",
                        "backend",
                        "TummlyBackend"
                    )
                );

            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider
            {
                get;
                set;
            } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        }
    }
}

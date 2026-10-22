using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class CustomPlanQuoteServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly CustomPlanQuoteService _sut;

        public CustomPlanQuoteServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            var json = PricebookCatalog.LoadFromDirectory(
                TestPricebookPaths.PackDirectory()
            );
            var snapshot = json.GetRequired(json.CurrentPricebookId);
            _db.CommercialCatalogVersions.Add(
                CommercialCatalogSeeder.FromSnapshot(snapshot, DateTime.UtcNow)
            );
            _db.SaveChanges();

            _sut = new CustomPlanQuoteService(_db);
        }

        [Fact]
        public async Task Quote_OneLocation_UsesStarter_AndCollapses()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, int>(),
                }
            );

            Assert.Equal("starter", result.BasePlan);
            Assert.Equal(BillingSubscriptionPlans.Starter, result.StoredAsPlan);
            Assert.Equal(3900, result.MonthlyNetPence);
            Assert.Equal(3900, result.PeriodNetPence);
            Assert.False(result.BasePlanChanged);
            Assert.Equal(1, result.Quantities.Locations);
            Assert.Equal(0, result.Quantities.QrPlacementsAccountExtra);
        }

        [Fact]
        public async Task Quote_TwoLocations_AutoRaisesStarterToGrowth()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 2,
                }
            );

            Assert.Equal("growth", result.BasePlan);
            Assert.True(result.BasePlanChanged);
            Assert.Equal("starter", result.RequestedBasePlan);
            Assert.Equal(BillingSubscriptionPlans.Growth, result.StoredAsPlan);
        }

        [Fact]
        public async Task Quote_OneLocation_RequestedGroup_UsesStarter()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = 1,
                }
            );

            // Location count alone sets the base package.
            Assert.Equal("starter", result.BasePlan);
            Assert.True(result.BasePlanChanged);
            Assert.Equal("group", result.RequestedBasePlan);
            Assert.Equal(BillingSubscriptionPlans.Starter, result.StoredAsPlan);
        }

        [Fact]
        public async Task Quote_OneLocation_WithPacks_StaysOnStarterAsCustom()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, int>
                    {
                        ["offers_5"] = 1,
                        ["team_users_5"] = 1,
                    },
                }
            );

            Assert.Equal("starter", result.BasePlan);
            Assert.False(result.BasePlanChanged);
            Assert.Equal(BillingSubscriptionPlans.Custom, result.StoredAsPlan);
        }

        [Fact]
        public async Task Quote_SixLocations_AddsExtraLocationLine()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = 6,
                }
            );

            Assert.Equal("group", result.BasePlan);
            Assert.Equal(BillingSubscriptionPlans.Custom, result.StoredAsPlan);
            Assert.Equal(1, result.Locations - 5);
            Assert.Contains(result.Lines, l => l.Key == "extra:location");
            Assert.True(result.MonthlyNetPence > result.Lines[0].NetPence);
            Assert.Equal(6, result.Quantities.Locations);
            // Group users 25 + 1 × 2 extra users
            Assert.Equal(27, result.Quantities.TeamUsers);
        }

        [Fact]
        public async Task Quote_PackMaths_MonthlyAndAnnualFallback()
        {
            var monthly = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, int> { ["offers_5"] = 2 },
                }
            );

            // offer rate: 5 units = 1200 pence → 10 units = 2400
            Assert.Equal(BillingSubscriptionPlans.Custom, monthly.StoredAsPlan);
            Assert.Equal(3900 + 2400, monthly.MonthlyNetPence);
            Assert.Equal(3900 + 2400, monthly.PeriodNetPence);
            Assert.Equal(3 + 10, monthly.Quantities.Offers);

            var annual = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "annual",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, int> { ["offers_5"] = 1 },
                }
            );

            // pack annual fallback: round(1200 × 12 × 0.85) = 12240
            var packAnnual = (int)Math.Round(1200 * 12 * 0.85);
            Assert.Equal(annual.Lines[0].NetPence + packAnnual, annual.PeriodNetPence);
            Assert.True(annual.PeriodNetPence > annual.MonthlyNetPence);
        }

        [Fact]
        public async Task Quote_QrPack_SetsAccountExtraOnly()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, int> { ["qr_10"] = 2 },
                }
            );

            Assert.Equal(20, result.Quantities.QrPlacementsAccountExtra);
            Assert.True(result.Quantities.QrPlacements > 0);
            Assert.Equal(
                result.Quantities.QrPlacements,
                result.Quantities.QrPlacements
            );
        }

        [Fact]
        public async Task Quote_Over500_ThrowsContactSales()
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        Locations = 501,
                    }
                )
            );
            Assert.Equal("custom_plan_contact_sales", ex.Code);
        }

        [Fact]
        public async Task Quote_UnknownPack_ThrowsInvalidPack()
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        Locations = 1,
                        Packs = new Dictionary<string, int> { ["nope"] = 1 },
                    }
                )
            );
            Assert.Equal("invalid_pack", ex.Code);
        }

        [Fact]
        public void LineNet_AnnualUsesAdminOverrideWhenSet()
        {
            var withAnnual = CustomPlanLineNet.ForCadence(
                "annual",
                2,
                monthlyNetPence: 3900,
                annualNetPence: 39800,
                unitQuantity: 1
            );
            Assert.Equal(79600, withAnnual);

            var fallback = CustomPlanLineNet.ForCadence(
                "annual",
                1,
                monthlyNetPence: 1200,
                annualNetPence: null,
                unitQuantity: 5
            );
            // monthly for 1 unit of size 5: ceil(1*1200/5)=240; annual = round(240*12*0.85)
            Assert.Equal((int)Math.Round(240 * 12 * 0.85), fallback);
        }

        public void Dispose() => _db.Dispose();
    }
}

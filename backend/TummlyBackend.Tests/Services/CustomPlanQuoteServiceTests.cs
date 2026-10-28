using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
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

            _sut = new CustomPlanQuoteService(_db, Options.Create(new TummlySellerVatSettings()));
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
                    Packs = new Dictionary<string, decimal>(),
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
                    Packs = new Dictionary<string, decimal>
                    {
                        ["offer"] = 1,
                        ["team_user"] = 1,
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
            Assert.Contains(result.Lines, l => l.Key == "extra:location:band_1");
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
                    Packs = new Dictionary<string, decimal> { ["offer"] = 10 },
                }
            );

            // offer rate: £2.40 per unit → 10 units = 2400
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
                    Packs = new Dictionary<string, decimal> { ["offer"] = 5 },
                }
            );

            // annual fallback: round(1200 × 12 × 0.85) = 12240 for 5 units
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
                    Packs = new Dictionary<string, decimal> { ["qr_placement"] = 20 },
                }
            );

            Assert.Equal(20, result.Quantities.QrPlacementsAccountExtra);
            Assert.True(result.Quantities.QrPlacements > 0);
            // Starter base 10 + 20 purchased units, one Workspace pool.
            Assert.Equal(30, result.WorkspaceQrPool);
        }

        [Theory]
        // plan base, extra paid Group Locations, purchased units -> Workspace pool
        [InlineData("starter", 1, 0, 10)]
        [InlineData("starter", 1, 7, 17)]
        [InlineData("growth", 2, 13, 38)]
        [InlineData("growth", 2, 0, 25)]
        [InlineData("group", 5, 0, 50)]
        // Group: 50 + 10 x (6 - 5) + 2.
        [InlineData("group", 6, 2, 62)]
        // Group: 50 + 10 x (40 - 5) + 20.
        [InlineData("group", 40, 20, 420)]
        [InlineData("group", 500, 0, 5000)]
        public async Task Quote_WorkspaceQrPool_IsBaseAllowancePlusLocationsPlusPurchasedUnits(
            string basePlan,
            int locations,
            int qrUnits,
            int expectedPool
        )
        {
            var quote = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = basePlan,
                    Locations = locations,
                    Packs = new Dictionary<string, decimal> { ["qr_placement"] = qrUnits },
                }
            );

            Assert.Equal(basePlan, quote.BasePlan);
            Assert.Equal(expectedPool, quote.WorkspaceQrPool);
        }

        [Theory]
        [InlineData("starter", 1, 0)]
        [InlineData("starter", 1, 7)]
        [InlineData("growth", 2, 13)]
        [InlineData("group", 6, 2)]
        [InlineData("group", 40, 20)]
        public async Task Quote_WorkspaceQrPool_EqualsThePoolShopAndCaptureRead(
            string basePlan,
            int locations,
            int qrUnits
        )
        {
            var quote = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = basePlan,
                    Locations = locations,
                    Packs = new Dictionary<string, decimal> { ["qr_placement"] = qrUnits },
                }
            );

            // The saved config is what Capture and Shop read through IActiveQrCapacity.
            var account = new BillingAccount
            {
                SubscriptionPlan = BillingSubscriptionPlans.Custom,
                ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                CustomPlanConfigJson = quote.ToConfig().ToJson(),
            };
            var capacity = new ActiveQrCapacity(
                _db,
                PricebookCatalog.LoadFromDirectory(TestPricebookPaths.PackDirectory())
            );

            var allowance = await capacity.GetAllowanceAsync(account, CancellationToken.None);

            Assert.NotNull(allowance);
            Assert.Equal(quote.WorkspaceQrPool, allowance!.WorkspacePool);
        }

        [Theory]
        [InlineData("team_user", 1, 300, 3060)]
        [InlineData("team_user", 2, 600, 6120)]
        [InlineData("team_user", 7, 2100, 21420)]
        [InlineData("team_user", 13, 3900, 39780)]
        [InlineData("offer", 1, 240, 2448)]
        [InlineData("offer", 2, 480, 4896)]
        [InlineData("offer", 7, 1680, 17136)]
        [InlineData("offer", 13, 3120, 31824)]
        [InlineData("qr_placement", 1, 60, 612)]
        [InlineData("qr_placement", 2, 120, 1224)]
        [InlineData("qr_placement", 7, 420, 4284)]
        [InlineData("qr_placement", 13, 780, 7956)]
        public async Task Quote_SingleUnits_ChargeUnitRateTimesQuantity(
            string packKey,
            int units,
            int expectedMonthlyPence,
            int expectedAnnualPence
        )
        {
            var monthly = await QuoteWithAsync("monthly", packKey, units);
            var annual = await QuoteWithAsync("annual", packKey, units);

            var monthlyLine = Assert.Single(monthly.Lines, l => l.Key == $"pack:{packKey}");
            var annualLine = Assert.Single(annual.Lines, l => l.Key == $"pack:{packKey}");
            Assert.Equal(units, monthlyLine.Quantity);
            Assert.Equal(expectedMonthlyPence, monthlyLine.NetPence);
            Assert.Equal(expectedAnnualPence, annualLine.NetPence);
            Assert.Equal(3900 + expectedMonthlyPence, monthly.PeriodNetPence);
        }

        [Fact]
        public async Task Quote_SingleUnits_AddExactQuantitiesToEntitlements()
        {
            var quote = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, decimal>
                    {
                        ["team_user"] = 7,
                        ["offer"] = 13,
                        ["qr_placement"] = 2,
                    },
                }
            );

            var baseline = await QuoteWithAsync("monthly", "offer", 0);
            Assert.Equal(baseline.Quantities.TeamUsers + 7, quote.Quantities.TeamUsers);
            Assert.Equal(baseline.Quantities.Offers + 13, quote.Quantities.Offers);
            Assert.Equal(2, quote.Quantities.QrPlacementsAccountExtra);
        }

        [Theory]
        [InlineData("team_users_5", "team_user")]
        [InlineData("offers_5", "offer")]
        [InlineData("qr_10", "qr_placement")]
        public async Task Quote_LegacyCapacityPack_IsRejectedNotConverted(
            string legacyKey,
            string replacement
        )
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "starter",
                        Locations = 1,
                        Packs = new Dictionary<string, decimal> { [legacyKey] = 1 },
                    }
                )
            );

            Assert.Equal("legacy_pack_key", ex.Code);
            Assert.Contains(replacement, ex.Message);
        }

        [Fact]
        public async Task Quote_LegacyPackWithZeroCount_IsStillRejected()
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "starter",
                        Locations = 1,
                        Packs = new Dictionary<string, decimal> { ["qr_10"] = 0 },
                    }
                )
            );

            Assert.Equal("legacy_pack_key", ex.Code);
        }

        [Fact]
        public async Task Quote_MaxCapacityUnits_AcceptedAndOneMoreRejected()
        {
            var atMax = await QuoteWithAsync(
                "monthly",
                "team_user",
                CustomPlanPacks.MaxCapacityUnits
            );
            Assert.Equal(
                CustomPlanPacks.MaxCapacityUnits * 300,
                atMax.Lines.Single(l => l.Key == "pack:team_user").NetPence
            );

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                QuoteWithAsync("monthly", "team_user", CustomPlanPacks.MaxCapacityUnits + 1)
            );
            Assert.Equal("invalid_pack_quantity", ex.Code);
        }

        [Theory]
        [InlineData("team_user")]
        [InlineData("offer")]
        [InlineData("qr_placement")]
        public async Task Quote_PilotBasePlan_RejectsPaidCapacityUnits(string packKey)
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "pilot",
                        Locations = 1,
                        Packs = new Dictionary<string, decimal> { [packKey] = 1 },
                    }
                )
            );

            Assert.Equal("pilot_no_capacity_addons", ex.Code);
        }

        [Theory]
        [InlineData("monthly", 0)]
        [InlineData("monthly", 2000)]
        [InlineData("annual", 2000)]
        public async Task Quote_ReturnsVatGrossRecurringDueNowAndEffectiveTiming(
            string cadence,
            int vatActiveBps
        )
        {
            var sut = new CustomPlanQuoteService(
                _db,
                Options.Create(new TummlySellerVatSettings { IsActive = vatActiveBps > 0 })
            );

            var quote = await sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = cadence,
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, decimal> { ["team_user"] = 7 },
                }
            );

            var net = quote.PeriodNetPence;
            var expectedVat = vatActiveBps > 0 ? (int)Math.Round(net * 0.2m, MidpointRounding.AwayFromZero) : 0;
            Assert.Equal(vatActiveBps, quote.VatRateBps);
            Assert.Equal(expectedVat, quote.VatAmountPence);
            Assert.Equal(net + expectedVat, quote.GrossTotalPence);
            Assert.Equal(quote.GrossTotalPence, quote.RecurringGrossPence);
            Assert.Equal(quote.GrossTotalPence, quote.DueNowGrossPence);
            Assert.Equal(CustomPlanEffectiveTimings.OnPaymentConfirmed, quote.EffectiveTiming);
        }

        [Fact]
        public async Task Quote_HashCoversAnyChangeToUnitsOrCadence()
        {
            var a = await QuoteWithAsync("monthly", "team_user", 7);
            var sameAgain = await QuoteWithAsync("monthly", "team_user", 7);
            var moreUnits = await QuoteWithAsync("monthly", "team_user", 8);
            var annual = await QuoteWithAsync("annual", "team_user", 7);

            Assert.Equal(a.QuoteHash, sameAgain.QuoteHash);
            Assert.NotEqual(a.QuoteHash, moreUnits.QuoteHash);
            Assert.NotEqual(a.QuoteHash, annual.QuoteHash);
        }

        [Fact]
        public void CapacityPacks_AreSingleUnits()
        {
            foreach (var key in new[] { "team_user", "offer", "qr_placement" })
            {
                Assert.Equal(1, CustomPlanPacks.ByKey[key].PackSize);
            }

            Assert.False(CustomPlanPacks.IsKnownPackKey("team_users_5"));
            Assert.False(CustomPlanPacks.IsKnownPackKey("offers_5"));
            Assert.False(CustomPlanPacks.IsKnownPackKey("qr_10"));
        }

        private Task<CustomPlanQuoteResult> QuoteWithAsync(
            string cadence,
            string packKey,
            int units
        ) =>
            _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = cadence,
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, decimal> { [packKey] = units },
                }
            );

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
                        Packs = new Dictionary<string, decimal> { ["nope"] = 1 },
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

        [Theory]
        [InlineData(5, 19900)]
        [InlineData(6, 23800)]
        [InlineData(30, 117400)]
        [InlineData(31, 121100)]
        [InlineData(50, 191400)]
        [InlineData(51, 194900)]
        [InlineData(100, 366400)]
        [InlineData(101, 369700)]
        [InlineData(250, 861400)]
        [InlineData(251, 864500)]
        [InlineData(500, 1636400)]
        public async Task Quote_MonthlyFixtures(int locations, int expectedMonthlyPence)
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = locations,
                }
            );

            Assert.Equal(expectedMonthlyPence, result.MonthlyNetPence);
            Assert.Equal(expectedMonthlyPence, result.PeriodNetPence);
        }

        [Fact]
        public async Task Quote_Annual500_Exact()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "annual",
                    BasePlan = "group",
                    Locations = 500,
                }
            );

            Assert.Equal(16691800, result.PeriodNetPence);
        }

        [Fact]
        public async Task Quote_31Locations_HasTwoBandLines()
        {
            var result = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = 31,
                }
            );

            var band1 = result.Lines.Single(l => l.Key == "extra:location:band_1");
            Assert.Equal(25, band1.Quantity);
            Assert.Equal(97500, band1.NetPence);

            var band2 = result.Lines.Single(l => l.Key == "extra:location:band_2");
            Assert.Equal(1, band2.Quantity);
            Assert.Equal(3700, band2.NetPence);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(2.5)]
        public async Task Quote_InvalidLocations_ReturnsInvalidLocationCount(double locations)
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "group",
                        Locations = (decimal)locations,
                    }
                )
            );
            Assert.Equal("invalid_location_count", ex.Code);
        }

        [Fact]
        public async Task Quote_501Locations_ReturnsContactSales()
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "group",
                        Locations = 501,
                    }
                )
            );
            Assert.Equal("custom_plan_contact_sales", ex.Code);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1.5)]
        [InlineData(501)]
        public async Task Quote_InvalidPackQuantity_ReturnsInvalidPackQuantity(double quantity)
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "starter",
                        Locations = 1,
                        Packs = new Dictionary<string, decimal>
                        {
                            ["offer"] = (decimal)quantity,
                        },
                    }
                )
            );
            Assert.Equal("invalid_pack_quantity", ex.Code);
        }

        [Fact]
        public async Task Quote_UnknownPackBeforeBadQuantity_ReturnsInvalidPack()
        {
            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "starter",
                        Locations = 1,
                        Packs = new Dictionary<string, decimal>
                        {
                            ["offer"] = -1,
                            ["nope"] = 1,
                        },
                    }
                )
            );
            Assert.Equal("invalid_pack", ex.Code);
        }

        [Fact]
        public async Task Quote_Sms5000_GroupOk_GrowthRejected()
        {
            var group = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = 5,
                    Packs = new Dictionary<string, decimal> { ["sms_5000"] = 1 },
                }
            );
            Assert.Equal("group", group.BasePlan);
            Assert.Equal(1, group.Packs["sms_5000"]);

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "group",
                        Locations = 2,
                        Packs = new Dictionary<string, decimal> { ["sms_5000"] = 1 },
                    }
                )
            );
            Assert.Equal("sms_pack_requires_approval", ex.Code);
        }

        [Fact]
        public async Task Quote_MissingBand3_Over50Fails_Under50Ok()
        {
            var band3 = _db.CommercialCustomUnitRateRows.Single(r =>
                r.RateKey == CommercialCustomUnitRateKeys.ExtraLocationBand3
            );
            _db.CommercialCustomUnitRateRows.Remove(band3);
            await _db.SaveChangesAsync();

            var under = await _sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = 50,
                }
            );
            Assert.Equal(191400, under.MonthlyNetPence);

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "monthly",
                        BasePlan = "group",
                        Locations = 51,
                    }
                )
            );
            Assert.Equal("catalog_incomplete", ex.Code);
        }

        [Fact]
        public async Task Quote_MissingAnnualBand2_Returns_CatalogIncomplete()
        {
            var band2 = _db.CommercialCustomUnitRateRows.Single(r =>
                r.RateKey == CommercialCustomUnitRateKeys.ExtraLocationBand2
            );
            band2.AnnualNetPence = null;
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<CustomPlanQuoteException>(() =>
                _sut.QuoteAsync(
                    new CustomPlanQuoteRequest
                    {
                        Cadence = "annual",
                        BasePlan = "group",
                        Locations = 31,
                    }
                )
            );
            Assert.Equal("catalog_incomplete", ex.Code);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 2000)]
        public async Task Quote_VatRateBps_FollowsSellerVatSetting(
            bool sellerVatActive,
            int expectedBps
        )
        {
            // The catalogue row holds 2000 in both cases; the seller setting decides.
            Assert.Equal(2000, _db.CommercialCatalogVersions.Single().VatRateBps);
            var sut = new CustomPlanQuoteService(
                _db,
                Options.Create(
                    new TummlySellerVatSettings { IsActive = sellerVatActive }
                )
            );

            var result = await sut.QuoteAsync(
                new CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "group",
                    Locations = 31,
                }
            );

            Assert.Equal(expectedBps, result.VatRateBps);
        }

        [Fact]
        public async Task Quote_HashStable_AndChangesWithPrice()
        {
            var request = new CustomPlanQuoteRequest
            {
                Cadence = "monthly",
                BasePlan = "group",
                Locations = 31,
            };

            var first = await _sut.QuoteAsync(request);
            var second = await _sut.QuoteAsync(request);
            Assert.Matches("^[0-9a-f]{32}$", first.QuoteHash);
            Assert.Equal(first.QuoteHash, second.QuoteHash);

            var band2 = _db.CommercialCustomUnitRateRows.Single(r =>
                r.RateKey == CommercialCustomUnitRateKeys.ExtraLocationBand2
            );
            band2.MonthlyNetPence = 3600;
            await _db.SaveChangesAsync();

            var changed = await _sut.QuoteAsync(request);
            Assert.NotEqual(first.QuoteHash, changed.QuoteHash);
        }

        [Fact]
        public async Task Quote_HashChanges_WhenOnlyVatRateChanges()
        {
            var request = new CustomPlanQuoteRequest
            {
                Cadence = "monthly",
                BasePlan = "group",
                Locations = 31,
            };
            var withoutVat = await new CustomPlanQuoteService(
                _db,
                Options.Create(new TummlySellerVatSettings { IsActive = false })
            ).QuoteAsync(request);
            var withVat = await new CustomPlanQuoteService(
                _db,
                Options.Create(new TummlySellerVatSettings { IsActive = true })
            ).QuoteAsync(request);

            Assert.Equal(withoutVat.PeriodNetPence, withVat.PeriodNetPence);
            Assert.Equal(0, withoutVat.VatRateBps);
            Assert.Equal(2000, withVat.VatRateBps);
            Assert.NotEqual(withoutVat.QuoteHash, withVat.QuoteHash);
        }

        public void Dispose() => _db.Dispose();
    }
}

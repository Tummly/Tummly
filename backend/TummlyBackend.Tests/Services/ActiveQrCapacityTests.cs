using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class ActiveQrCapacityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ActiveQrCapacity _capacity;
        private int _restaurantId;
        private int _locationId;

        public ActiveQrCapacityTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _capacity = new ActiveQrCapacity(
                _context,
                PricebookCatalog.LoadFromDirectory(TestPricebookPaths.PackDirectory())
            );

            SeedWorkspace();
        }

        [Fact]
        public async Task CountUsed_ActiveOnly_EqualsActiveCount()
        {
            await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);
            await SeedQrAsync(QrType.TableTent, QrCodeStatus.Active);
            await SeedQrAsync(QrType.WindowSticker, QrCodeStatus.Paused);

            var used = await _capacity.CountUsedForRestaurantAsync(_restaurantId, CancellationToken.None);

            Assert.Equal(2, used);
        }

        [Fact]
        public async Task CountUsed_TwoHeldReservationsSameType_CountOnce()
        {
            await SeedReservationAsync(_locationId, QrType.GuestCard, ShopQrSlotReservationStatuses.Open);
            await SeedReservationAsync(_locationId, QrType.GuestCard, ShopQrSlotReservationStatuses.Open);

            var used = await _capacity.CountUsedForRestaurantAsync(_restaurantId, CancellationToken.None);

            Assert.Equal(1, used);
        }

        [Fact]
        public async Task CountUsed_HeldReservationWhereActiveCodeExists_NotCounted()
        {
            await SeedQrAsync(QrType.TableTent, QrCodeStatus.Active);
            await SeedReservationAsync(_locationId, QrType.TableTent, ShopQrSlotReservationStatuses.Open);

            var used = await _capacity.CountUsedForRestaurantAsync(_restaurantId, CancellationToken.None);

            Assert.Equal(1, used);
        }

        [Fact]
        public async Task CountUsed_ReviewStatusCountsAsHeld()
        {
            await SeedReservationAsync(_locationId, QrType.TableTalker, ShopQrSlotReservationStatuses.Review);

            var used = await _capacity.CountUsedForRestaurantAsync(_restaurantId, CancellationToken.None);

            Assert.Equal(1, used);
        }

        [Fact]
        public async Task CountUsed_ReleasedAndConverted_NotCounted()
        {
            await SeedReservationAsync(_locationId, QrType.GuestCard, ShopQrSlotReservationStatuses.Released);
            await SeedReservationAsync(_locationId, QrType.TableTent, ShopQrSlotReservationStatuses.Converted);

            var used = await _capacity.CountUsedForRestaurantAsync(_restaurantId, CancellationToken.None);

            Assert.Equal(0, used);
        }

        [Fact]
        public async Task EvaluateAsync_HeldReservationTakesSlot_NoLongerFitsAtCap()
        {
            var probe = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, ct: CancellationToken.None);
            Assert.True(probe.EntitlementsAvailable);
            var pool = probe.WorkspacePool;
            Assert.True(pool >= 1);

            var beforeReservation = await _capacity.EvaluateAsync(_restaurantId, _locationId, pool, ct: CancellationToken.None);
            Assert.True(beforeReservation.Fits);
            Assert.Equal(0, beforeReservation.Used);

            await SeedReservationAsync(_locationId, QrType.GuestCard, ShopQrSlotReservationStatuses.Open);

            var afterReservation = await _capacity.EvaluateAsync(_restaurantId, _locationId, pool, ct: CancellationToken.None);
            Assert.False(afterReservation.Fits);
            Assert.Equal(1, afterReservation.Used);
            Assert.Equal(pool, afterReservation.WorkspacePool);
        }

        [Fact]
        public async Task EvaluateAsync_WorkspacePool_CountsHeldReservationAtOtherLocation()
        {
            // B4: Growth pool is 25. An Open reservation at Location B counts for a check at A.
            await UsePlanAsync(BillingSubscriptionPlans.Growth);
            var otherLocationId = await AddLocationAsync("Second site");
            await SeedQrsAsync(_locationId, 23);

            var before = await _capacity.EvaluateAsync(_restaurantId, _locationId, 2, ct: CancellationToken.None);
            Assert.True(before.Fits);
            Assert.Equal(25, before.WorkspacePool);
            Assert.Equal(23, before.Used);

            await SeedReservationAsync(otherLocationId, QrType.GuestCard, ShopQrSlotReservationStatuses.Open);

            var after = await _capacity.EvaluateAsync(_restaurantId, _locationId, 2, ct: CancellationToken.None);
            Assert.False(after.Fits);
            Assert.Equal(24, after.Used);
            Assert.Equal(25, after.WorkspacePool);

            var one = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, ct: CancellationToken.None);
            Assert.True(one.Fits);
        }

        [Fact]
        public async Task EvaluateAsync_ConvertedReservation_KeepsOneCount()
        {
            // B4: pay converts the held slot into the Active code; the count stays one.
            await UsePlanAsync(BillingSubscriptionPlans.Growth);
            var otherLocationId = await AddLocationAsync("Second site");
            var held = await SeedReservationAsync(otherLocationId, QrType.GuestCard, ShopQrSlotReservationStatuses.Open);
            Assert.Equal(1, await _capacity.CountUsedForRestaurantAsync(_restaurantId, CancellationToken.None));

            _context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = otherLocationId,
                QrType = QrType.GuestCard,
                Token = Guid.NewGuid().ToString("N"),
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            });
            held.Status = ShopQrSlotReservationStatuses.Converted;
            await _context.SaveChangesAsync();

            Assert.Equal(1, await _capacity.CountUsedForRestaurantAsync(_restaurantId, CancellationToken.None));
        }

        [Fact]
        public async Task EvaluateAsync_HeldSlotTakeover_DoesNotDoubleCountAcrossWorkspace()
        {
            await UsePlanAsync(BillingSubscriptionPlans.Growth);
            var otherLocationId = await AddLocationAsync("Second site");
            await SeedQrsAsync(_locationId, 24);
            await SeedReservationAsync(otherLocationId, QrType.GuestCard, ShopQrSlotReservationStatuses.Open);
            await SeedQrAsync(otherLocationId, QrType.GuestCard, QrCodeStatus.Paused);

            // Workspace is full (24 + 1 held). Activating the paused code takes over the held slot.
            var takeover = await _capacity.EvaluateAsync(
                _restaurantId, otherLocationId, 1, QrType.GuestCard, CancellationToken.None);
            Assert.True(takeover.Fits);

            var plain = await _capacity.EvaluateAsync(_restaurantId, otherLocationId, 1, ct: CancellationToken.None);
            Assert.False(plain.Fits);
        }

        [Fact]
        public async Task EvaluateAsync_WorkspacePool_BlocksNewSlotWhenOtherLocationFilledPool()
        {
            // A1: Pilot pool is 5. Location A uses all five; Location B has none of its own but cannot add.
            await UsePlanAsync(BillingSubscriptionPlans.Pilot);
            var otherLocationId = await AddLocationAsync("Second site");
            await SeedQrsAsync(_locationId, 5);

            var atA = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, ct: CancellationToken.None);
            Assert.False(atA.Fits);
            Assert.Equal(5, atA.Used);
            Assert.Equal(5, atA.WorkspacePool);

            var atB = await _capacity.EvaluateAsync(_restaurantId, otherLocationId, 1, ct: CancellationToken.None);
            Assert.False(atB.Fits);
            Assert.Equal(5, atB.Used);
            Assert.Equal(5, atB.WorkspacePool);
        }

        [Fact]
        public async Task EvaluateAsync_Growth_DistributionAcrossLocations_26thRefusedAnywhere()
        {
            // B2: 20 at A, 3 at B, 2 at C are all valid under Growth 25.
            await UsePlanAsync(BillingSubscriptionPlans.Growth);
            var b = await AddLocationAsync("B");
            var c = await AddLocationAsync("C");
            await SeedQrsAsync(_locationId, 20);
            await SeedQrsAsync(b, 3);
            await SeedQrsAsync(c, 2);

            foreach (var locationId in new[] { _locationId, b, c })
            {
                var decision = await _capacity.EvaluateAsync(_restaurantId, locationId, 1, ct: CancellationToken.None);
                Assert.True(decision.EntitlementsAvailable);
                Assert.False(decision.Fits);
                Assert.Equal(25, decision.WorkspacePool);
                Assert.Equal(25, decision.Used);
            }
        }

        [Fact]
        public async Task EvaluateAsync_PausedAndArchived_DoNotCount_ReactivationCountsAgain()
        {
            // B3
            await UsePlanAsync(BillingSubscriptionPlans.Pilot);
            await SeedQrsAsync(_locationId, 3);
            await SeedQrAsync(_locationId, QrType.TableTent, QrCodeStatus.Paused);
            await SeedQrAsync(_locationId, QrType.WindowSticker, QrCodeStatus.Paused);
            await SeedQrAsync(_locationId, QrType.OfferCard, QrCodeStatus.Archived);
            await SeedQrAsync(_locationId, QrType.CounterCard, QrCodeStatus.Archived);

            var two = await _capacity.EvaluateAsync(_restaurantId, _locationId, 2, ct: CancellationToken.None);
            Assert.Equal(3, two.Used);
            Assert.True(two.Fits);

            var paused = await _context.QrCodes.FirstAsync(q => q.Status == QrCodeStatus.Paused);
            paused.Status = QrCodeStatus.Active;
            await _context.SaveChangesAsync();

            var afterReactivate = await _capacity.EvaluateAsync(_restaurantId, _locationId, 2, ct: CancellationToken.None);
            Assert.Equal(4, afterReactivate.Used);
            Assert.False(afterReactivate.Fits);
        }

        [Theory]
        [InlineData("Free", 0, 0)]
        [InlineData("Pilot", 0, 5)]
        [InlineData("Starter", 0, 10)]
        [InlineData("Growth", 0, 25)]
        [InlineData("Group", 0, 50)]
        [InlineData("Group", 3, 80)]
        [InlineData("Group", 25, 300)]
        // Only Group earns +10 per paid Location; other plans never read the count.
        [InlineData("Starter", 3, 10)]
        [InlineData("Growth", 3, 25)]
        [InlineData("Pilot", 3, 5)]
        public async Task GetAllowance_StandardPlans_FollowCommercialV4(
            string plan,
            int paidExtraLocations,
            int expectedPool
        )
        {
            await UsePlanAsync(plan, paidExtraLocations);
            var account = await _context.BillingAccounts.AsNoTracking().SingleAsync();

            var allowance = await _capacity.GetAllowanceAsync(account, CancellationToken.None);

            Assert.NotNull(allowance);
            Assert.Equal(expectedPool, allowance!.WorkspacePool);
        }

        [Theory]
        // Group base, 40 Locations, 20 qr_placement units: 50 + 10 x (40 - 5) + 20.
        [InlineData("group", 40, 50, 20, 420)]
        [InlineData("group", 5, 50, 0, 50)]
        [InlineData("group", 3, 50, 0, 50)]
        // Starter and Growth bases add nothing for extra Locations.
        [InlineData("starter", 3, 10, 0, 10)]
        [InlineData("starter", 3, 10, 20, 30)]
        [InlineData("growth", 3, 25, 0, 25)]
        [InlineData("growth", 8, 25, 10, 35)]
        public async Task GetAllowance_CustomV2_FollowsCommercialV4(
            string basePlan,
            int locations,
            int qrPlacements,
            int accountExtra,
            int expectedPool
        )
        {
            await UseCustomPlanAsync(basePlan, locations, qrPlacements, accountExtra);
            var account = await _context.BillingAccounts.AsNoTracking().SingleAsync();

            var allowance = await _capacity.GetAllowanceAsync(account, CancellationToken.None);

            Assert.NotNull(allowance);
            Assert.Equal(expectedPool, allowance!.WorkspacePool);
        }

        [Theory]
        // Starter 10 + 2 purchased qr_placement units: the 13th active code is refused.
        [InlineData(11, true)]
        [InlineData(12, false)]
        public async Task EvaluateAsync_PurchasedQrUnitsFromAQuote_RaiseTheWorkspacePool(
            int alreadyActive,
            bool expectedFits
        )
        {
            var snapshot = PricebookCatalog
                .LoadFromDirectory(TestPricebookPaths.PackDirectory());
            _context.CommercialCatalogVersions.Add(
                CommercialCatalogSeeder.FromSnapshot(
                    snapshot.GetRequired(snapshot.CurrentPricebookId),
                    DateTime.UtcNow
                )
            );
            await _context.SaveChangesAsync();
            var quote = await new CustomPlanQuoteService(
                _context,
                Microsoft.Extensions.Options.Options.Create(
                    new TummlyBackend.Configurations.TummlySellerVatSettings()
                )
            ).QuoteAsync(
                new TummlyBackend.DTOs.Admin.CustomPlanQuoteRequest
                {
                    Cadence = "monthly",
                    BasePlan = "starter",
                    Locations = 1,
                    Packs = new Dictionary<string, decimal> { ["qr_placement"] = 2 },
                }
            );
            var billing = await _context.BillingAccounts.SingleAsync(row => row.RestaurantId == _restaurantId);
            billing.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            billing.CustomPlanConfigJson = quote.ToConfig().ToJson();
            await _context.SaveChangesAsync();
            await SeedQrsAsync(_locationId, alreadyActive);

            var decision = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, ct: CancellationToken.None);

            Assert.Equal(12, decision.WorkspacePool);
            Assert.Equal(expectedFits, decision.Fits);
        }

        [Fact]
        public async Task GetAllowance_CustomPlanBeforeSettlement_IsUnavailable_EvenWithPurchasedUnits()
        {
            // Signup writes plan Custom and the quote before checkout; status stays Free
            // until the verified payment. Paid units must not count before that.
            await UseCustomPlanAsync(basePlan: "starter", locations: 1, qrPlacements: 10, accountExtra: 5);
            var billing = await _context.BillingAccounts.SingleAsync(row => row.RestaurantId == _restaurantId);
            billing.BillingStatus = BillingStatuses.Free;
            await _context.SaveChangesAsync();

            var allowance = await _capacity.GetAllowanceAsync(billing, CancellationToken.None);
            var decision = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, ct: CancellationToken.None);

            Assert.Null(allowance);
            Assert.False(decision.EntitlementsAvailable);
            Assert.False(decision.Fits);
        }

        [Fact]
        public async Task GetAllowance_CustomPlanAfterSettlement_CountsPurchasedUnits()
        {
            await UseCustomPlanAsync(basePlan: "starter", locations: 1, qrPlacements: 10, accountExtra: 5);
            var billing = await _context.BillingAccounts.SingleAsync(row => row.RestaurantId == _restaurantId);
            billing.BillingStatus = BillingStatuses.Active;
            await _context.SaveChangesAsync();

            var allowance = await _capacity.GetAllowanceAsync(billing, CancellationToken.None);

            Assert.Equal(15, allowance!.WorkspacePool);
        }

        [Fact]
        public async Task GetAllowance_CustomV1_NoBasePlan_IsPlacementsPlusExtra()
        {
            await UseCustomPlanAsync(basePlan: null, locations: 40, qrPlacements: 7, accountExtra: 3);
            var account = await _context.BillingAccounts.AsNoTracking().SingleAsync();

            var allowance = await _capacity.GetAllowanceAsync(account, CancellationToken.None);

            Assert.Equal(10, allowance!.WorkspacePool);
        }

        [Fact]
        public async Task GetAllowance_CustomV2_UnknownBasePlan_FailsClosed()
        {
            await UseCustomPlanAsync("group", 6, 50, 0);
            var billing = await _context.BillingAccounts.SingleAsync();
            billing.ContractedPricebookId = "NO-SUCH-PRICEBOOK";
            await _context.SaveChangesAsync();

            var allowance = await _capacity.GetAllowanceAsync(billing, CancellationToken.None);

            Assert.Null(allowance);
        }

        [Fact]
        public void WorkspaceQrPool_Compute_Overflow_Throws()
        {
            Assert.Throws<OverflowException>(() =>
                WorkspaceQrPool.Compute(int.MaxValue, 0, 1));
        }

        [Theory]
        [InlineData(int.MaxValue, 5)]
        [InlineData(10, int.MaxValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public async Task GetAllowance_CustomHugeQuantity_FailsClosed_NoException(
            int qrPlacements,
            int accountExtra
        )
        {
            await UseCustomPlanAsync(basePlan: null, locations: 1, qrPlacements, accountExtra);
            var account = await _context.BillingAccounts.AsNoTracking().SingleAsync();

            var allowance = await _capacity.GetAllowanceAsync(account, CancellationToken.None);
            var decision = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, ct: CancellationToken.None);

            Assert.Null(allowance);
            Assert.False(decision.EntitlementsAvailable);
            Assert.False(decision.Fits);
        }

        [Fact]
        public async Task GetAllowance_CustomV2Group_IncludedLocationsComeFromQuoteCatalog()
        {
            // The quote catalog says Group includes 10 Locations; the contracted pricebook says 5.
            await SeedCatalogAsync("QUOTE-CATALOG-A", groupIncludedLocations: 10);
            await UseCustomPlanAsync("group", 40, 50, 0, catalogPublicId: "QUOTE-CATALOG-A");
            var account = await _context.BillingAccounts.AsNoTracking().SingleAsync();

            var allowance = await _capacity.GetAllowanceAsync(account, CancellationToken.None);

            // 50 + 10 x (40 - 10) from the catalog; the pricebook would give 50 + 10 x 35 = 400.
            Assert.Equal(350, allowance!.WorkspacePool);
        }

        [Fact]
        public async Task GetAllowance_CustomV2Group_CatalogMissing_FallsBackToPricebook()
        {
            await UseCustomPlanAsync("group", 40, 50, 0, catalogPublicId: "NO-SUCH-CATALOG");
            var account = await _context.BillingAccounts.AsNoTracking().SingleAsync();

            var allowance = await _capacity.GetAllowanceAsync(account, CancellationToken.None);

            Assert.Equal(400, allowance!.WorkspacePool);
        }

        private async Task SeedCatalogAsync(string publicId, int groupIncludedLocations)
        {
            var versionId = Guid.NewGuid();
            _context.CommercialCatalogVersions.Add(new CommercialCatalogVersion
            {
                Id = versionId,
                PublicId = publicId,
                Status = CommercialCatalogStatuses.Published,
                PublishedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Plans =
                [
                    new CommercialPlanRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = versionId,
                        PlanKey = "group",
                        DisplayName = "Group",
                        IncludedLocations = groupIncludedLocations,
                        ActiveQrPlacementsPerLocation = 50,
                    },
                ],
            });
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task EvaluateAsync_NoEntitlements_ReportsUnavailable()
        {
            var billing = await _context.BillingAccounts.SingleAsync(row => row.RestaurantId == _restaurantId);
            billing.ContractedPricebookId = string.Empty;
            await _context.SaveChangesAsync();

            var decision = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, ct: CancellationToken.None);

            Assert.False(decision.EntitlementsAvailable);
            Assert.False(decision.Fits);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedWorkspace()
        {
            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                0,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            BillingCreditsService.ApplyPilotSignupBilling(
                billing,
                DateTime.UtcNow
            );
            var restaurant = new Restaurant
            {
                Name = "Capacity Restaurant",
                AccountType = "Single",
                OwnerUserId = 0,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = billing,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();

            _restaurantId = restaurant.Id;
            _locationId = location.Id;
        }

        private async Task UsePlanAsync(string plan, int paidExtraLocations = 0)
        {
            var billing = await _context.BillingAccounts.SingleAsync(row => row.RestaurantId == _restaurantId);
            billing.SubscriptionPlan = plan;
            billing.PaidExtraLocationCount = paidExtraLocations;
            await _context.SaveChangesAsync();
        }

        private async Task UseCustomPlanAsync(
            string? basePlan,
            int locations,
            int qrPlacements,
            int accountExtra,
            string catalogPublicId = ""
        )
        {
            var billing = await _context.BillingAccounts.SingleAsync(row => row.RestaurantId == _restaurantId);
            billing.SubscriptionPlan = BillingSubscriptionPlans.Custom;
            billing.CustomPlanConfigJson = new CustomPlanConfig
            {
                Version = basePlan == null ? 1 : 2,
                CatalogPublicId = catalogPublicId,
                BasePlan = basePlan,
                Cadence = "monthly",
                Locations = locations,
                Quantities = new CustomPlanQuantities
                {
                    Locations = locations,
                    QrPlacements = qrPlacements,
                    QrPlacementsAccountExtra = accountExtra,
                },
                MonthlyNetPence = 3900,
                PeriodNetPence = 3900,
            }.ToJson();
            await _context.SaveChangesAsync();
        }

        private async Task<int> AddLocationAsync(string name)
        {
            var location = new RestaurantLocation
            {
                RestaurantId = _restaurantId,
                LocationName = name,
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private Task SeedQrAsync(QrType qrType, QrCodeStatus status) =>
            SeedQrAsync(_locationId, qrType, status);

        private async Task SeedQrAsync(int locationId, QrType qrType, QrCodeStatus status)
        {
            _context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = qrType,
                Token = Guid.NewGuid().ToString("N"),
                Status = status,
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();
        }

        /// <summary>Adds <paramref name="count"/> Active Digital Guest Links at the Location.</summary>
        private async Task SeedQrsAsync(int locationId, int count)
        {
            for (var i = 0; i < count; i++)
            {
                _context.QrCodes.Add(new QrCode
                {
                    RestaurantLocationId = locationId,
                    QrType = QrType.DigitalGuestLink,
                    Token = Guid.NewGuid().ToString("N"),
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<ShopQrSlotReservation> SeedReservationAsync(int locationId, QrType qrType, string status)
        {
            var now = DateTime.UtcNow;
            var reservation = new ShopQrSlotReservation
            {
                Id = Guid.NewGuid(),
                ShopOrderId = Guid.NewGuid(),
                RestaurantLocationId = locationId,
                QrType = qrType,
                Status = status,
                ReconcileAfterUtc = now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            _context.ShopQrSlotReservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }
    }
}

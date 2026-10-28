using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// A Location in premises review (PendingReview or Rejected) cannot be
    /// activated, start a Pilot, get a complimentary Starter Kit, or hold
    /// active QR capacity. One test per activation path.
    /// </summary>
    public class PremisesReviewGatingTests : IDisposable
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook = TestPricebookPaths.LoadV3();
        private readonly ComplimentaryStarterShopOrderService _complimentary;
        private readonly LocationsLifecycleWriteService _write;
        private readonly LocationLifecycleService _lifecycle;
        private readonly CaptureQrLifecycleService _capture;
        private readonly ActiveQrCapacity _capacity;
        private readonly PilotStartService _pilot;
        private int _userId;
        private int _restaurantId;

        public PremisesReviewGatingTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            ShopMaterialsCatalogSeed
                .EnsureSeededAsync(_context)
                .GetAwaiter()
                .GetResult();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            var smartGuestLink = new SmartGuestLinkService(
                _context,
                configuration,
                new NoOpBillingAccountLifecycle()
            );
            _capacity = TestActiveQrCapacity.For(_context);
            var qrProvisioning = new QrCodeProvisioningService(
                _context,
                smartGuestLink,
                _capacity
            );
            _complimentary = new ComplimentaryStarterShopOrderService(
                _context,
                new DbMaterialsCatalog(_context),
                new ShopOrderNumberAllocator(_context)
            );
            _write = new LocationsLifecycleWriteService(
                _context,
                qrProvisioning,
                _complimentary,
                NoOpPrintReadyQrMaterialsWork.Instance,
                new RestaurantAccountTypePromotion(_context),
                _pricebook,
                _capacity
            );
            _capture = new CaptureQrLifecycleService(
                _context,
                smartGuestLink,
                _pricebook,
                null!
            );
            _lifecycle = new LifecycleServiceFactory(_context, _capture, _pricebook).Create();
            _pilot = PilotStartTestFactory.Create(
                _context,
                _pricebook,
                new CreditLedgerService(
                    _context,
                    TimeProvider.System,
                    _pricebook,
                    new AdminAuditService(_context, TimeProvider.System)
                ),
                NoOpComplimentaryStarterShopOrderService.Instance,
                NoOpPrintReadyQrMaterialsWork.Instance,
                new RecordingPilotStartedEmailService()
            );

            _restaurantId = SeedWorkspace(BillingSubscriptionPlans.Group);
        }

        // --- Activate (Draft -> Active) ---

        [Theory]
        [InlineData(PremisesReviewStatus.PendingReview)]
        [InlineData(PremisesReviewStatus.Rejected)]
        public async Task ActivateDraft_WhileInReview_IsRefused_AndStaysDraft(
            PremisesReviewStatus status
        )
        {
            var id = SeedLocation(LocationLifecycleStatus.Draft, status);

            var result = await _write.ActivateDraftAsync(_restaurantId, id, _userId);

            var conflict = Assert.IsType<LocationLifecycleWriteResult.Conflict>(result);
            Assert.Equal(PremisesReviewGate.BlockedMessage, conflict.Message);
            Assert.Equal(LocationLifecycleStatus.Draft, (await Reload(id)).LifecycleStatus);
            Assert.Empty(_context.ShopOrders);
            Assert.Empty(_context.QrCodes);
        }

        [Theory]
        [InlineData(PremisesReviewStatus.None)]
        [InlineData(PremisesReviewStatus.Cleared)]
        public async Task ActivateDraft_NotInReview_Activates(PremisesReviewStatus status)
        {
            var id = SeedLocation(LocationLifecycleStatus.Draft, status);

            var result = await _write.ActivateDraftAsync(_restaurantId, id, _userId);

            Assert.IsType<LocationLifecycleWriteResult.Ok>(result);
            Assert.Equal(LocationLifecycleStatus.Active, (await Reload(id)).LifecycleStatus);
        }

        [Fact]
        public async Task ActivateDraft_AfterClear_MintsQrCodes_AndIssuesStarterKit()
        {
            var id = SeedLocation(LocationLifecycleStatus.Draft, PremisesReviewStatus.Cleared);

            await _write.ActivateDraftAsync(_restaurantId, id, _userId);

            Assert.True(await _context.QrCodes.AnyAsync(qr => qr.RestaurantLocationId == id));
            Assert.Single(_context.ShopOrders.Where(order => order.IsComplimentary));
        }

        // --- Resume (Paused -> Active) ---

        [Fact]
        public async Task Resume_WhileInReview_IsRefused()
        {
            var id = SeedLocation(
                LocationLifecycleStatus.Paused,
                PremisesReviewStatus.PendingReview
            );

            var result = await _lifecycle.ResumeAsync(
                new LocationLifecycleCommand
                {
                    UserId = _userId,
                    RestaurantId = _restaurantId,
                    LocationId = id,
                }
            );

            Assert.Equal(LocationLifecycleResultKind.InvalidTransition, result.Kind);
            Assert.Equal(PremisesReviewGate.BlockedMessage, result.Message);
            Assert.Equal(LocationLifecycleStatus.Paused, (await Reload(id)).LifecycleStatus);
        }

        // --- Complimentary Starter Kit ---

        [Fact]
        public async Task ComplimentaryKit_ForLocationInReview_IsNotIssued()
        {
            // Even an Active row (for example a legacy flag) must not get a kit.
            var id = SeedLocation(
                LocationLifecycleStatus.Active,
                PremisesReviewStatus.PendingReview
            );

            var single = await _complimentary.EnsureForLocationAsync(
                _restaurantId,
                id,
                _userId,
                "Owner"
            );
            var all = await _complimentary.EnsureAllActiveLocationsAsync(
                _restaurantId,
                _userId,
                "Owner"
            );

            Assert.Equal(Guid.Empty, single.ShopOrderId);
            Assert.Empty(all);
            Assert.Empty(_context.ShopOrders);
        }

        [Fact]
        public async Task ComplimentaryKit_AfterClear_IsIssued()
        {
            var id = SeedLocation(LocationLifecycleStatus.Active, PremisesReviewStatus.Cleared);

            var single = await _complimentary.EnsureForLocationAsync(
                _restaurantId,
                id,
                _userId,
                "Owner"
            );

            Assert.NotEqual(Guid.Empty, single.ShopOrderId);
        }

        // --- Pilot ---

        [Theory]
        [InlineData(PremisesReviewStatus.PendingReview)]
        [InlineData(PremisesReviewStatus.Rejected)]
        public async Task StartPilot_WorkspaceWithLocationInReview_IsRefused_NoPilotNoCredits(
            PremisesReviewStatus status
        )
        {
            var freeId = SeedWorkspace(BillingSubscriptionPlans.Free);
            SeedLocation(LocationLifecycleStatus.Draft, status, freeId);

            var result = await _pilot.StartPilotAsync(freeId);

            Assert.False(result.Started);
            Assert.Equal(PremisesReviewGate.PilotRefusedCode, result.RefusedCode);
            var account = await _context.BillingAccounts.SingleAsync(row => row.RestaurantId == freeId);
            Assert.Null(account.PilotStartedAtUtc);
            Assert.Equal(BillingSubscriptionPlans.Free, account.SubscriptionPlan);
            Assert.Equal(0, await _context.CreditLedgerEntries.CountAsync(row => row.RestaurantId == freeId));
        }

        [Fact]
        public async Task StartPilot_AfterClear_Starts()
        {
            var freeId = SeedWorkspace(BillingSubscriptionPlans.Free);
            SeedLocation(LocationLifecycleStatus.Draft, PremisesReviewStatus.Cleared, freeId);

            var result = await _pilot.StartPilotAsync(freeId);

            Assert.True(result.Started);
            Assert.Null(result.RefusedCode);
        }

        // --- QR capacity ---

        [Fact]
        public async Task QrActivation_AtLocationInReview_IsRefused()
        {
            var id = SeedLocation(
                LocationLifecycleStatus.Draft,
                PremisesReviewStatus.PendingReview
            );
            var qr = SeedQr(id, QrCodeStatus.Paused);

            var result = await _capture.ResumeAsync(
                new QrCodeLifecycleCommand
                {
                    UserId = _userId,
                    LocationId = id,
                    QrCodeId = qr.Id,
                }
            );

            Assert.Equal(QrLifecycleResultKind.Conflict, result.Kind);
            Assert.Equal(PremisesReviewGate.BlockedMessage, result.Message);
            Assert.Equal(QrCodeStatus.Paused, (await _context.QrCodes.SingleAsync(row => row.Id == qr.Id)).Status);
        }

        [Fact]
        public async Task CapacityEvaluate_ForLocationInReview_DoesNotFit()
        {
            var id = SeedLocation(
                LocationLifecycleStatus.Draft,
                PremisesReviewStatus.PendingReview
            );

            var decision = await _capacity.EvaluateAsync(_restaurantId, id, 1);

            Assert.True(decision.EntitlementsAvailable);
            Assert.False(decision.Fits);
        }

        [Fact]
        public async Task CapacityEvaluate_ForClearedLocation_Fits()
        {
            var id = SeedLocation(LocationLifecycleStatus.Draft, PremisesReviewStatus.Cleared);

            var decision = await _capacity.EvaluateAsync(_restaurantId, id, 1);

            Assert.True(decision.Fits);
        }

        [Fact]
        public async Task PlanPausedCodes_AtLocationInReview_AreNotActivated()
        {
            var blocked = SeedLocation(
                LocationLifecycleStatus.Draft,
                PremisesReviewStatus.PendingReview
            );
            var clear = SeedLocation(LocationLifecycleStatus.Active, PremisesReviewStatus.None);
            var blockedQr = SeedQr(blocked, QrCodeStatus.Paused);
            var clearQr = SeedQr(clear, QrCodeStatus.Paused);

            await _capture.ActivatePlanPausedCodesAsync(_restaurantId);

            Assert.Equal(
                QrCodeStatus.Paused,
                (await _context.QrCodes.SingleAsync(row => row.Id == blockedQr.Id)).Status
            );
            Assert.Equal(
                QrCodeStatus.Active,
                (await _context.QrCodes.SingleAsync(row => row.Id == clearQr.Id)).Status
            );
        }

        [Fact]
        public async Task MintDefaultQrCodes_ForLocationInReview_MintsNothing()
        {
            var id = SeedLocation(
                LocationLifecycleStatus.Draft,
                PremisesReviewStatus.PendingReview
            );
            var location = await Reload(id);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example",
                })
                .Build();
            var provisioning = new QrCodeProvisioningService(
                _context,
                new SmartGuestLinkService(_context, configuration, new NoOpBillingAccountLifecycle()),
                _capacity
            );

            var minted = await provisioning.MintDefaultQrCodesAsync(location);
            await _context.SaveChangesAsync();

            Assert.Empty(minted);
            Assert.Empty(_context.QrCodes);
        }

        public void Dispose() => _context.Dispose();

        private async Task<RestaurantLocation> Reload(int id)
        {
            _context.ChangeTracker.Clear();
            return await _context.RestaurantLocations.SingleAsync(row => row.Id == id);
        }

        private QrCode SeedQr(int locationId, QrCodeStatus status)
        {
            var qr = new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = QrType.CounterCard,
                Token = Guid.NewGuid().ToString("N")[..32],
                Status = status,
                CreatedAt = DateTime.UtcNow,
            };
            _context.QrCodes.Add(qr);
            _context.SaveChanges();
            return qr;
        }

        private int SeedLocation(
            LocationLifecycleStatus lifecycle,
            PremisesReviewStatus review,
            int? restaurantId = null
        )
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId ?? _restaurantId,
                LocationName = $"Venue {Guid.NewGuid():N}",
                Address = $"{Random.Shared.Next(1, 900)} Test Street",
                City = "Leeds",
                Postcode = "LS1 1AA",
                LifecycleStatus = lifecycle,
                PremisesReviewStatus = review,
                CaptureLocationStatus = CaptureLocationStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();
            return location.Id;
        }

        private int SeedWorkspace(string plan)
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            _userId = user.Id;

            var restaurant = new Restaurant
            {
                Name = "Gate Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                BillingContactUserId = user.Id,
                PrivacyContactUserId = user.Id,
                SupportContactUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                PricebookId
            );
            billing.SubscriptionPlan = plan;
            if (plan != BillingSubscriptionPlans.Free)
            {
                billing.BillingStatus = BillingStatuses.Active;
            }

            _context.BillingAccounts.Add(billing);
            _context.SaveChanges();
            return restaurant.Id;
        }

        private sealed class LifecycleServiceFactory
        {
            private readonly ApplicationDbContext _db;
            private readonly ICaptureQrLifecycleService _captureLifecycle;
            private readonly IPricebookCatalog _book;

            public LifecycleServiceFactory(
                ApplicationDbContext db,
                ICaptureQrLifecycleService captureLifecycle,
                IPricebookCatalog book
            )
            {
                _db = db;
                _captureLifecycle = captureLifecycle;
                _book = book;
            }

            public LocationLifecycleService Create() =>
                new(_db, _captureLifecycle, new RestaurantAccountTypePromotion(_db), _book);
        }
    }
}

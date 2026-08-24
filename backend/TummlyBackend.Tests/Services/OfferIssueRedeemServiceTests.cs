using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="OfferIssueService"/> Check + Mark as redeemed
    /// (ticket 38 / 05).
    /// </summary>
    public class OfferIssueRedeemServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OfferIssueService _service;
        private readonly DateTime _now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

        public OfferIssueRedeemServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new OfferIssueService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CheckClaimCode_Valid_ReturnsPreviewWithoutClaimRequired()
        {
            var seeded = await SeedRedeemableIssueAsync(
                claimCode: "TUM-ABCDEF",
                claimedAt: null
            );

            var result = await _service.CheckClaimCodeAsync(
                seeded.LocationId,
                "tum-abcdef",
                _now
            );

            var ok = Assert.IsType<OfferRedeemCheckResult.Ok>(result);
            Assert.Equal(seeded.IssueId.ToString(), ok.Preview.IssueId);
            Assert.Equal("10% off next visit", ok.Preview.OfferTitle);
            Assert.Equal("Maya", ok.Preview.GuestName);
            Assert.Equal("Soho", ok.Preview.ValidAt);
            Assert.Equal("Single-use", ok.Preview.Usage);
            Assert.Contains("Apply 10%", ok.Preview.StaffInstruction);
            Assert.False(string.IsNullOrWhiteSpace(ok.Preview.Expires));
            Assert.Empty(await _context.OfferRedeemFailedAttempts.ToListAsync());
        }

        [Fact]
        public async Task CheckClaimCode_NotFound_IsInvalid_DoesNotWriteFailedAttempt()
        {
            var seeded = await SeedLocationGuestAndOfferAsync();

            var result = await _service.CheckClaimCodeAsync(
                seeded.LocationId,
                "TUM-ZZZZZZ",
                _now
            );

            var failed = Assert.IsType<OfferRedeemCheckResult.Failed>(result);
            Assert.Equal(OfferRedeemFailureReasons.Invalid, failed.Reason);
            Assert.Empty(await _context.OfferRedeemFailedAttempts.ToListAsync());
        }

        [Fact]
        public async Task CheckClaimCode_Expired_WritesFailedAttempt()
        {
            var seeded = await SeedRedeemableIssueAsync(
                claimCode: "TUM-EXPIRE",
                expiryAt: _now.AddMinutes(-1)
            );

            var result = await _service.CheckClaimCodeAsync(
                seeded.LocationId,
                "TUM-EXPIRE",
                _now
            );

            var failed = Assert.IsType<OfferRedeemCheckResult.Failed>(result);
            Assert.Equal(OfferRedeemFailureReasons.Expired, failed.Reason);
            var attempt = Assert.Single(await _context.OfferRedeemFailedAttempts.ToListAsync());
            Assert.Equal(seeded.CatalogOfferId, attempt.CatalogOfferId);
            Assert.Equal(seeded.LocationId, attempt.RestaurantLocationId);
            Assert.Equal("TUM-EXPIRE", attempt.ClaimCode);
            Assert.Equal(OfferRedeemFailureReasons.Expired, attempt.Reason);
            Assert.Equal(_now, attempt.AttemptedAtUtc);
        }

        [Fact]
        public async Task CheckClaimCode_AlreadyUsed_WritesFailedAttempt()
        {
            var seeded = await SeedRedeemableIssueAsync(
                claimCode: "TUM-USED01",
                redeemedAt: _now.AddHours(-1)
            );

            var result = await _service.CheckClaimCodeAsync(
                seeded.LocationId,
                "TUM-USED01",
                _now
            );

            var failed = Assert.IsType<OfferRedeemCheckResult.Failed>(result);
            Assert.Equal(OfferRedeemFailureReasons.AlreadyUsed, failed.Reason);
            Assert.Equal(1, await _context.OfferRedeemFailedAttempts.CountAsync());
        }

        [Fact]
        public async Task CheckClaimCode_Voided_WritesFailedAttempt()
        {
            var seeded = await SeedRedeemableIssueAsync(
                claimCode: "TUM-VOID01",
                cancelledAt: _now.AddHours(-2)
            );

            var result = await _service.CheckClaimCodeAsync(
                seeded.LocationId,
                "TUM-VOID01",
                _now
            );

            var failed = Assert.IsType<OfferRedeemCheckResult.Failed>(result);
            Assert.Equal(OfferRedeemFailureReasons.Voided, failed.Reason);
            Assert.Equal(1, await _context.OfferRedeemFailedAttempts.CountAsync());
        }

        [Fact]
        public async Task CheckClaimCode_WrongLocation_WritesFailedAttempt()
        {
            var seeded = await SeedRedeemableIssueAsync(claimCode: "TUM-OTHER1");
            var otherLocationId = await SeedSecondLocationAsync(seeded.RestaurantId);

            var result = await _service.CheckClaimCodeAsync(
                otherLocationId,
                "TUM-OTHER1",
                _now
            );

            var failed = Assert.IsType<OfferRedeemCheckResult.Failed>(result);
            Assert.Equal(OfferRedeemFailureReasons.WrongLocation, failed.Reason);
            var attempt = Assert.Single(await _context.OfferRedeemFailedAttempts.ToListAsync());
            Assert.Equal(otherLocationId, attempt.RestaurantLocationId);
            Assert.Equal(seeded.CatalogOfferId, attempt.CatalogOfferId);
        }

        [Fact]
        public async Task RedeemClaimCode_PersistsRedeemedAt_WithoutPriorClaim()
        {
            var seeded = await SeedRedeemableIssueAsync(
                claimCode: "TUM-REDEEM",
                claimedAt: null
            );

            var result = await _service.RedeemClaimCodeAsync(
                seeded.LocationId,
                "TUM-REDEEM",
                seeded.IssueId.ToString(),
                _now
            );

            Assert.IsType<OfferRedeemMarkResult.Ok>(result);
            var issue = await _context.OfferIssues.SingleAsync(i => i.Id == seeded.IssueId);
            Assert.Equal(_now, issue.RedeemedAtUtc);
            Assert.Null(issue.ClaimedAtUtc);
        }

        [Fact]
        public async Task RedeemClaimCode_AlreadyUsed_Fails()
        {
            var seeded = await SeedRedeemableIssueAsync(
                claimCode: "TUM-AGAIN1",
                redeemedAt: _now.AddMinutes(-5)
            );

            var result = await _service.RedeemClaimCodeAsync(
                seeded.LocationId,
                "TUM-AGAIN1",
                seeded.IssueId.ToString(),
                _now
            );

            var failed = Assert.IsType<OfferRedeemMarkResult.Failed>(result);
            Assert.Equal(OfferRedeemFailureReasons.AlreadyUsed, failed.Reason);
        }

        [Fact]
        public async Task RedeemClaimCode_MismatchedIssueId_FailsAsInvalid()
        {
            var seeded = await SeedRedeemableIssueAsync(claimCode: "TUM-MISMATCH");

            var result = await _service.RedeemClaimCodeAsync(
                seeded.LocationId,
                "TUM-MISMATCH",
                "99999",
                _now
            );

            var failed = Assert.IsType<OfferRedeemMarkResult.Failed>(result);
            Assert.Equal(OfferRedeemFailureReasons.Invalid, failed.Reason);
            var issue = await _context.OfferIssues.SingleAsync(i => i.Id == seeded.IssueId);
            Assert.Null(issue.RedeemedAtUtc);
        }

        [Fact]
        public async Task CheckAndRedeem_FailWhileWorkspacePaused_SucceedAfterResume_CatalogUnchanged()
        {
            var seeded = await SeedRedeemableIssueAsync(claimCode: "TUM-PAUSE1");
            var restaurant = await _context.Restaurants.SingleAsync(
                r => r.Id == seeded.RestaurantId
            );
            var offerBefore = await _context.CatalogOffers.SingleAsync(
                o => o.Id == seeded.CatalogOfferId
            );
            var offerStatusBefore = offerBefore.Status;

            restaurant.WorkspaceStatus = WorkspaceStatus.Paused;
            await _context.SaveChangesAsync();

            var checkPaused = await _service.CheckClaimCodeAsync(
                seeded.LocationId,
                "TUM-PAUSE1",
                _now
            );
            var checkFailed = Assert.IsType<OfferRedeemCheckResult.Failed>(checkPaused);
            Assert.Equal(
                OfferRedeemFailureReasons.WorkspacePaused,
                checkFailed.Reason
            );

            var redeemPaused = await _service.RedeemClaimCodeAsync(
                seeded.LocationId,
                "TUM-PAUSE1",
                seeded.IssueId.ToString(),
                _now
            );
            var redeemFailed = Assert.IsType<OfferRedeemMarkResult.Failed>(
                redeemPaused
            );
            Assert.Equal(
                OfferRedeemFailureReasons.WorkspacePaused,
                redeemFailed.Reason
            );

            var issueWhilePaused = await _context.OfferIssues.SingleAsync(
                i => i.Id == seeded.IssueId
            );
            Assert.Null(issueWhilePaused.RedeemedAtUtc);

            restaurant.WorkspaceStatus = WorkspaceStatus.Active;
            await _context.SaveChangesAsync();

            var checkOk = await _service.CheckClaimCodeAsync(
                seeded.LocationId,
                "TUM-PAUSE1",
                _now
            );
            Assert.IsType<OfferRedeemCheckResult.Ok>(checkOk);

            var redeemOk = await _service.RedeemClaimCodeAsync(
                seeded.LocationId,
                "TUM-PAUSE1",
                seeded.IssueId.ToString(),
                _now
            );
            Assert.IsType<OfferRedeemMarkResult.Ok>(redeemOk);

            var offerAfter = await _context.CatalogOffers.SingleAsync(
                o => o.Id == seeded.CatalogOfferId
            );
            Assert.Equal(offerStatusBefore, offerAfter.Status);
            Assert.Equal(WorkspaceStatus.Active, restaurant.WorkspaceStatus);
        }

        private async Task<(
            int LocationId,
            int RestaurantId,
            int LocationGuestId,
            int CatalogOfferId,
            int IssueId
        )> SeedRedeemableIssueAsync(
            string claimCode,
            DateTime? claimedAt = null,
            DateTime? redeemedAt = null,
            DateTime? cancelledAt = null,
            DateTime? expiryAt = null
        )
        {
            var seeded = await SeedLocationGuestAndOfferAsync();
            var issue = new OfferIssue
            {
                CatalogOfferId = seeded.CatalogOfferId,
                LocationGuestId = seeded.LocationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = _now.AddDays(-1),
                ClaimedAtUtc = claimedAt,
                RedeemedAtUtc = redeemedAt,
                CancelledAtUtc = cancelledAt,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = expiryAt ?? _now.AddDays(7),
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off next visit",
                Description = "Come back soon",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                StaffInstructions =
                    "Apply 10% off the order before payment. Tap redeem once the discount has been applied.",
            };
            _context.OfferIssues.Add(issue);
            await _context.SaveChangesAsync();

            return (
                seeded.LocationId,
                seeded.RestaurantId,
                seeded.LocationGuestId,
                seeded.CatalogOfferId,
                issue.Id
            );
        }

        private async Task<(
            int LocationId,
            int RestaurantId,
            int LocationGuestId,
            int CatalogOfferId
        )> SeedLocationGuestAndOfferAsync()
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = _now,
                ActivatedAt = _now,
                ActivationExpiresAt = _now.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Redeem Restaurant",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = master.Id,
                Name = "Maya",
                CreatedAt = _now,
            };
            _context.LocationGuests.Add(lg);
            await _context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = "active",
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off next visit",
                Description = "Come back soon",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                StaffInstructions =
                    "Apply 10% off the order before payment. Tap redeem once the discount has been applied.",
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();

            return (location.Id, restaurant.Id, lg.Id, offer.Id);
        }

        private async Task<int> SeedSecondLocationAsync(int restaurantId)
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = "Camden",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }
    }
}

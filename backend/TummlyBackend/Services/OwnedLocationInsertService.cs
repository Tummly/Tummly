using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class OwnedLocationInsertService : IOwnedLocationInsertService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebookCatalog;

        public OwnedLocationInsertService(
            ApplicationDbContext context,
            IPricebookCatalog pricebookCatalog
        )
        {
            _context = context;
            _pricebookCatalog = pricebookCatalog;
        }

        public async Task<AddOwnedLocationResult> AddAsync(
            int restaurantId,
            AddOwnedLocationRequest request
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await LockBillingAccountRowAsync(restaurantId);

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);

            if (billingAccount == null)
            {
                await transaction.RollbackAsync();
                return new AddOwnedLocationResult.FailClosed();
            }

            if (
                !TryResolveEntitled(
                    billingAccount,
                    out var entitled
                )
            )
            {
                await transaction.RollbackAsync();
                return new AddOwnedLocationResult.FailClosed();
            }

            var ownedCount = await _context.RestaurantLocations
                .CountAsync(row => row.RestaurantId == restaurantId);

            if (ownedCount >= entitled)
            {
                await transaction.RollbackAsync();
                return new AddOwnedLocationResult.CapReached(
                    entitled,
                    ownedCount
                );
            }

            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = request.LocationName?.Trim() ?? "",
                Address = request.Address?.Trim() ?? "",
                Postcode = string.IsNullOrWhiteSpace(request.Postcode)
                    ? null
                    : UkPostcode.FormatForDisplay(request.Postcode),
                LocationPhone = PhoneNumberHelper.NormalizeOptional(
                    request.LocationPhone
                ),
                LocalContact = string.IsNullOrWhiteSpace(request.LocalContact)
                    ? null
                    : request.LocalContact.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AddOwnedLocationResult.Created(location.Id);
        }

        private bool TryResolveEntitled(
            BillingAccount billingAccount,
            out int entitled
        )
        {
            entitled = 0;
            try
            {
                var book = _pricebookCatalog.GetRequired(
                    billingAccount.ContractedPricebookId
                );
                return LocationCap.TryResolve(
                    book,
                    billingAccount.SubscriptionPlan,
                    billingAccount.PaidExtraLocationCount,
                    out entitled
                );
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private async Task LockBillingAccountRowAsync(int restaurantId)
        {
            if (!_context.Database.IsSqlServer())
            {
                return;
            }

            await _context.Database.ExecuteSqlRawAsync(
                """
                SELECT RestaurantId
                FROM BillingAccounts WITH (UPDLOCK, ROWLOCK)
                WHERE RestaurantId = {0}
                """,
                restaurantId
            );
        }
    }
}

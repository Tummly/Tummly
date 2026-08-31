using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> AccountLocks
            = new();

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
            int actorUserId,
            AddOwnedLocationRequest request
        )
        {
            var invalid = ValidateDraftFields(request);
            if (invalid != null)
            {
                return invalid;
            }

            if (!_context.Database.IsSqlServer())
            {
                var gate = AccountLocks.GetOrAdd(
                    restaurantId,
                    _ => new SemaphoreSlim(1, 1)
                );
                await gate.WaitAsync();
                try
                {
                    return await AddLockedAsync(
                        restaurantId,
                        actorUserId,
                        request
                    );
                }
                finally
                {
                    gate.Release();
                }
            }

            return await AddLockedAsync(restaurantId, actorUserId, request);
        }

        private async Task<AddOwnedLocationResult> AddLockedAsync(
            int restaurantId,
            int actorUserId,
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

            var actorDisplayName = await _context.Users
                .AsNoTracking()
                .Where(row => row.Id == actorUserId)
                .Select(row => row.FullName)
                .FirstOrDefaultAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = request.LocationName.Trim(),
                Address = request.Address.Trim(),
                City = request.City.Trim(),
                Postcode = UkPostcode.FormatForDisplay(request.Postcode!),
                LocationPhone = PhoneNumberHelper.NormalizeOptional(
                    request.LocationPhone
                ),
                LocalContact = string.IsNullOrWhiteSpace(request.LocalContact)
                    ? null
                    : request.LocalContact.Trim(),
                LifecycleStatus = LocationLifecycleStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            };

            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            _context.LocationActivities.Add(
                new LocationActivity
                {
                    RestaurantId = restaurantId,
                    LocationId = location.Id,
                    ActorUserId = actorUserId,
                    ActorDisplayName = string.IsNullOrWhiteSpace(actorDisplayName)
                        ? null
                        : actorDisplayName.Trim(),
                    Kind = LocationActivityKinds.LocationCreated,
                    Description = $"Created draft location “{location.LocationName}”.",
                    ToValue = LocationLifecycleStatus.Draft.ToString(),
                    OccurredAt = DateTime.UtcNow,
                }
            );
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AddOwnedLocationResult.Created(location.Id);
        }

        private static AddOwnedLocationResult.InvalidRequest? ValidateDraftFields(
            AddOwnedLocationRequest request
        )
        {
            if (string.IsNullOrWhiteSpace(request.LocationName))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "Location name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "Address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.City))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "City is required."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Postcode))
            {
                return new AddOwnedLocationResult.InvalidRequest(
                    "Postcode is required."
                );
            }

            return null;
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

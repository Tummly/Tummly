using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class CreditLedgerService : ICreditLedger
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> AccountLocks
            = new();

        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;

        public CreditLedgerService(
            ApplicationDbContext context,
            TimeProvider clock
        )
        {
            _context = context;
            _clock = clock;
        }

        public async Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
            CreditLedgerConsumeRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.Units <= 0)
            {
                return CreditLedgerWriteResult.Fail("insufficient_credits");
            }

            if (
                request.Channel is not (
                    CreditChannels.Ai
                    or CreditChannels.Email
                    or CreditChannels.Sms
                )
            )
            {
                return CreditLedgerWriteResult.Fail("insufficient_credits");
            }

            var gate = AccountLocks.GetOrAdd(
                request.RestaurantId,
                _ => new SemaphoreSlim(1, 1)
            );
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await ConsumeLockedAsync(request, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<CreditLedgerWriteResult> ConsumeLockedAsync(
            CreditLedgerConsumeRequest request,
            CancellationToken cancellationToken
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken
                );

            var locked = await LockBillingAccountAsync(
                request.RestaurantId,
                cancellationToken
            );
            if (!locked)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerWriteResult.Fail("insufficient_credits");
            }

            var locationOk = await _context.RestaurantLocations.AnyAsync(
                row =>
                    row.Id == request.LocationId
                    && row.RestaurantId == request.RestaurantId,
                cancellationToken
            );
            if (!locationOk)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerWriteResult.Fail("location_not_in_account");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var entries = await _context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == request.RestaurantId
                    && row.Channel == request.Channel
                )
                .ToListAsync(cancellationToken);

            var states = CreditLedgerCalculator.Project(entries, now);
            if (CreditLedgerCalculator.PoolAvailable(states) < request.Units)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerWriteResult.Fail("insufficient_credits");
            }

            var fills = CreditLedgerCalculator.Bind(states, request.Units);
            if (fills.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerWriteResult.Fail("insufficient_credits");
            }

            var inserted = new List<CreditLedgerEntry>(fills.Count);
            foreach (var fill in fills)
            {
                var row = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = request.RestaurantId,
                    Channel = request.Channel,
                    EntryType = CreditLedgerEntryTypes.Consumption,
                    Quantity = fill.Quantity,
                    AllocationId = fill.AllocationId,
                    ReservationRef = null,
                    LocationId = request.LocationId,
                    CreatedAtUtc = now,
                };
                inserted.Add(row);
                _context.CreditLedgerEntries.Add(row);
            }

            var postState = CreditLedgerCalculator.Project(
                [.. entries, .. inserted],
                now
            );
            if (!CreditLedgerCalculator.InvariantsHold(postState))
            {
                foreach (var row in inserted)
                {
                    _context.CreditLedgerEntries.Remove(row);
                }

                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerWriteResult.Fail("insufficient_credits");
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CreditLedgerWriteResult.Ok(
                inserted.Select(row => new CreditLedgerInsertedRow
                {
                    Id = row.Id,
                    AllocationId = row.AllocationId!.Value,
                    EntryType = row.EntryType,
                    Quantity = row.Quantity,
                    ReservationRef = row.ReservationRef,
                }).ToList()
            );
        }

        private async Task<bool> LockBillingAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM [BillingAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [RestaurantId] = {restaurantId}",
                    cancellationToken
                );
            }

            return await _context.BillingAccounts.AnyAsync(
                row => row.RestaurantId == restaurantId,
                cancellationToken
            );
        }
    }
}

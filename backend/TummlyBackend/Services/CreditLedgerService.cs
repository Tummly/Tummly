using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

            if (request.LocationId == null)
            {
                return CreditLedgerWriteResult.Fail("location_required");
            }

            if (!_context.Database.IsSqlServer())
            {
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

            return await ConsumeLockedAsync(request, cancellationToken);
        }

        public async Task<CreditLedgerWriteResult> StaffManualAdjustAsync(
            StaffManualAdjustRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var reason = request.Reason.Trim();
            if (reason.Length is < 1 or > 500)
            {
                return CreditLedgerWriteResult.Fail("reason_required");
            }

            if (request.Quantity <= 0)
            {
                return CreditLedgerWriteResult.Fail("invalid_quantity");
            }

            if (
                request.Channel is not (
                    CreditChannels.Ai
                    or CreditChannels.Email
                    or CreditChannels.Sms
                )
            )
            {
                return CreditLedgerWriteResult.Fail("invalid_channel");
            }

            var isGrant = string.Equals(
                request.Direction,
                StaffManualAdjustDirections.Grant,
                StringComparison.Ordinal
            );
            var isDebit = string.Equals(
                request.Direction,
                StaffManualAdjustDirections.Debit,
                StringComparison.Ordinal
            );
            if (!isGrant && !isDebit)
            {
                return CreditLedgerWriteResult.Fail("invalid_direction");
            }

            if (!_context.Database.IsSqlServer())
            {
                var gate = AccountLocks.GetOrAdd(
                    request.RestaurantId,
                    _ => new SemaphoreSlim(1, 1)
                );
                await gate.WaitAsync(cancellationToken);
                try
                {
                    return await StaffManualAdjustLockedAsync(
                        request,
                        reason,
                        isGrant,
                        cancellationToken
                    );
                }
                finally
                {
                    gate.Release();
                }
            }

            return await StaffManualAdjustLockedAsync(
                request,
                reason,
                isGrant,
                cancellationToken
            );
        }

        public async Task<CreditLedgerWriteResult> StaffReverseAsync(
            StaffReverseRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var reason = request.Reason.Trim();
            if (reason.Length is < 1 or > 500)
            {
                return CreditLedgerWriteResult.Fail("reason_required");
            }

            var target = await _context.CreditLedgerEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == request.ReversedEntryId,
                    cancellationToken
                );
            if (target == null)
            {
                return CreditLedgerWriteResult.Fail("entry_not_found");
            }

            if (!_context.Database.IsSqlServer())
            {
                var gate = AccountLocks.GetOrAdd(
                    target.RestaurantId,
                    _ => new SemaphoreSlim(1, 1)
                );
                await gate.WaitAsync(cancellationToken);
                try
                {
                    return await StaffReverseLockedAsync(
                        request,
                        reason,
                        target,
                        cancellationToken
                    );
                }
                finally
                {
                    gate.Release();
                }
            }

            return await StaffReverseLockedAsync(
                request,
                reason,
                target,
                cancellationToken
            );
        }

        private async Task<CreditLedgerWriteResult> StaffReverseLockedAsync(
            StaffReverseRequest request,
            string reason,
            CreditLedgerEntry target,
            CancellationToken cancellationToken
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken
                );

            var locked = await LockBillingAccountAsync(
                target.RestaurantId,
                cancellationToken
            );
            if (!locked)
            {
                return await AbortAsync(
                    transaction,
                    "restaurant_not_found",
                    cancellationToken
                );
            }

            var liveTarget = await _context.CreditLedgerEntries
                .FirstOrDefaultAsync(
                    row => row.Id == request.ReversedEntryId,
                    cancellationToken
                );
            if (liveTarget == null)
            {
                return await AbortAsync(
                    transaction,
                    "entry_not_found",
                    cancellationToken
                );
            }

            var alreadyReversed = await _context.CreditLedgerEntries.AnyAsync(
                row => row.ReversedEntryId == liveTarget.Id,
                cancellationToken
            );
            if (alreadyReversed)
            {
                return await AbortAsync(
                    transaction,
                    "already_reversed",
                    cancellationToken
                );
            }

            var isDebitManual =
                liveTarget.EntryType == CreditLedgerEntryTypes.ManualAdjustment
                && liveTarget.AllocationId != null;
            var isAllowed =
                liveTarget.EntryType is
                    CreditLedgerEntryTypes.Consumption
                    or CreditLedgerEntryTypes.Refund
                || isDebitManual;
            if (!isAllowed)
            {
                return await AbortAsync(
                    transaction,
                    "invalid_reversal_target",
                    cancellationToken
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var entries = await _context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == liveTarget.RestaurantId
                    && row.Channel == liveTarget.Channel
                )
                .ToListAsync(cancellationToken);

            var reversal = new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                RestaurantId = liveTarget.RestaurantId,
                Channel = liveTarget.Channel,
                EntryType = CreditLedgerEntryTypes.Reversal,
                Quantity = liveTarget.Quantity,
                AllocationId = liveTarget.AllocationId,
                ReversedEntryId = liveTarget.Id,
                ActorStaffUserId = request.ActorStaffUserId,
                Reason = reason,
                HelpCentreQueryId = request.HelpCentreQueryId,
                CreatedAtUtc = now,
            };
            _context.CreditLedgerEntries.Add(reversal);

            var postState = CreditLedgerCalculator.Project(
                [.. entries, reversal],
                now
            );
            if (!CreditLedgerCalculator.InvariantsHold(postState))
            {
                _context.CreditLedgerEntries.Remove(reversal);
                return await AbortAsync(
                    transaction,
                    "insufficient_credits",
                    cancellationToken
                );
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CreditLedgerWriteResult.Ok(
                [
                    new CreditLedgerInsertedRow
                    {
                        Id = reversal.Id,
                        AllocationId = reversal.AllocationId ?? reversal.Id,
                        EntryType = reversal.EntryType,
                        Quantity = reversal.Quantity,
                        ReservationRef = reversal.ReservationRef,
                    },
                ]
            );
        }

        private async Task<CreditLedgerWriteResult> StaffManualAdjustLockedAsync(
            StaffManualAdjustRequest request,
            string reason,
            bool isGrant,
            CancellationToken cancellationToken
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken
                );

            var billingAccount = await _context.BillingAccounts
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == request.RestaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                return await AbortAsync(
                    transaction,
                    "restaurant_not_found",
                    cancellationToken
                );
            }

            if (_context.Database.IsSqlServer())
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM [BillingAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [RestaurantId] = {request.RestaurantId}",
                    cancellationToken
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var entries = await _context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == request.RestaurantId
                    && row.Channel == request.Channel
                )
                .ToListAsync(cancellationToken);

            var states = CreditLedgerCalculator.Project(entries, now);
            var inserted = new List<CreditLedgerEntry>();

            if (isGrant)
            {
                var grantId = Guid.NewGuid();
                inserted.Add(
                    new CreditLedgerEntry
                    {
                        Id = grantId,
                        RestaurantId = request.RestaurantId,
                        Channel = request.Channel,
                        EntryType = CreditLedgerEntryTypes.ManualAdjustment,
                        Quantity = request.Quantity,
                        AllocationId = null,
                        PricebookVersion = billingAccount.ContractedPricebookId,
                        ExpiresAtUtc = now.AddMonths(12),
                        ActorStaffUserId = request.ActorStaffUserId,
                        Reason = reason,
                        HelpCentreQueryId = request.HelpCentreQueryId,
                        CreatedAtUtc = now,
                    }
                );
                _context.CreditLedgerEntries.Add(inserted[0]);
            }
            else
            {
                if (
                    request.AllocationId is Guid allocationId
                    && states.FirstOrDefault(row => row.Id == allocationId) is
                    {
                        Held: > 0,
                        Bindable: 0,
                    }
                )
                {
                    return await AbortAsync(
                        transaction,
                        "held_credits",
                        cancellationToken
                    );
                }

                var fills = CreditLedgerCalculator.BindDebit(
                    states,
                    request.Quantity,
                    request.AllocationId
                );
                if (fills.Count == 0)
                {
                    return await AbortAsync(
                        transaction,
                        "insufficient_credits",
                        cancellationToken
                    );
                }

                foreach (var fill in fills)
                {
                    var row = new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = request.RestaurantId,
                        Channel = request.Channel,
                        EntryType = CreditLedgerEntryTypes.ManualAdjustment,
                        Quantity = fill.Quantity,
                        AllocationId = fill.AllocationId,
                        ActorStaffUserId = request.ActorStaffUserId,
                        Reason = reason,
                        HelpCentreQueryId = request.HelpCentreQueryId,
                        CreatedAtUtc = now,
                    };
                    inserted.Add(row);
                    _context.CreditLedgerEntries.Add(row);
                }
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

                return await AbortAsync(
                    transaction,
                    "insufficient_credits",
                    cancellationToken
                );
            }

            BillingActivityWriter.TryAppend(
                _context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = request.RestaurantId,
                    Kind = BillingActivityKinds.ManualCreditAdjusted,
                    OccurredAtUtc = now,
                    ActorDisplayName = BillingActivityActors.TummlySupport,
                    Channel = request.Channel,
                    Qty = request.Quantity,
                    ManualAdjustDirection = isGrant
                        ? BillingManualAdjustDirections.Add
                        : BillingManualAdjustDirections.Remove,
                }
            );

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CreditLedgerWriteResult.Ok(
                inserted.Select(row => new CreditLedgerInsertedRow
                {
                    Id = row.Id,
                    AllocationId = row.AllocationId ?? row.Id,
                    EntryType = row.EntryType,
                    Quantity = row.Quantity,
                    ReservationRef = row.ReservationRef,
                }).ToList()
            );
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
                return await AbortAsync(
                    transaction,
                    "insufficient_credits",
                    cancellationToken
                );
            }

            var locationOk = await _context.RestaurantLocations.AnyAsync(
                row =>
                    row.Id == request.LocationId
                    && row.RestaurantId == request.RestaurantId,
                cancellationToken
            );
            if (!locationOk)
            {
                return await AbortAsync(
                    transaction,
                    "location_not_in_account",
                    cancellationToken
                );
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
                return await AbortAsync(
                    transaction,
                    "insufficient_credits",
                    cancellationToken
                );
            }

            var fills = CreditLedgerCalculator.Bind(states, request.Units);
            if (fills.Count == 0)
            {
                return await AbortAsync(
                    transaction,
                    "insufficient_credits",
                    cancellationToken
                );
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

                return await AbortAsync(
                    transaction,
                    "insufficient_credits",
                    cancellationToken
                );
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

        private static async Task<CreditLedgerWriteResult> AbortAsync(
            IDbContextTransaction transaction,
            string code,
            CancellationToken cancellationToken
        )
        {
            await transaction.RollbackAsync(cancellationToken);
            return CreditLedgerWriteResult.Fail(code);
        }
    }
}

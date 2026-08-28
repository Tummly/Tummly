using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TummlyBackend.Billing.Pricebook;
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
        private readonly IPricebookCatalog _pricebookCatalog;

        public CreditLedgerService(
            ApplicationDbContext context,
            TimeProvider clock,
            IPricebookCatalog pricebookCatalog
        )
        {
            _context = context;
            _clock = clock;
            _pricebookCatalog = pricebookCatalog;
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

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => ConsumeLockedAsync(request, cancellationToken),
                cancellationToken
            );
        }

        public async Task<CreditLedgerWriteResult> ReserveAsync(
            CreditLedgerReserveRequest request,
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

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => ReserveLockedAsync(request, cancellationToken),
                cancellationToken
            );
        }

        public async Task<CreditLedgerWriteResult> SettleAsync(
            CreditLedgerSettleRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.AcceptedUnits <= 0)
            {
                return CreditLedgerWriteResult.Ok([]);
            }

            if (
                request.Channel is not (
                    CreditChannels.Ai
                    or CreditChannels.Email
                    or CreditChannels.Sms
                )
            )
            {
                return CreditLedgerWriteResult.Fail("reservation_closed");
            }

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => SettleLockedAsync(request, cancellationToken),
                cancellationToken
            );
        }

        public async Task<CreditLedgerWriteResult> ReleaseAsync(
            CreditLedgerReleaseRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (
                request.Channel is not (
                    CreditChannels.Ai
                    or CreditChannels.Email
                    or CreditChannels.Sms
                )
            )
            {
                return CreditLedgerWriteResult.Ok([]);
            }

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => ReleaseLockedAsync(request, cancellationToken),
                cancellationToken
            );
        }

        public async Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
            CreditLedgerMintTopupRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.Quantity <= 0)
            {
                return CreditLedgerMintTopupResult.Fail("invalid_quantity");
            }

            if (
                request.Channel is not (
                    CreditChannels.Ai
                    or CreditChannels.Email
                    or CreditChannels.Sms
                )
            )
            {
                return CreditLedgerMintTopupResult.Fail("invalid_channel");
            }

            if (string.IsNullOrWhiteSpace(request.SourcePaymentRef))
            {
                return CreditLedgerMintTopupResult.Fail("source_payment_ref_required");
            }

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => MintTopupLockedAsync(request, cancellationToken),
                cancellationToken
            );
        }

        public async Task<CreditLedgerWriteResult> ReleaseHeldAsync(
            CreditLedgerReleaseHeldRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (request.Quantity <= 0)
            {
                return CreditLedgerWriteResult.Fail("invalid_quantity");
            }

            if (string.IsNullOrWhiteSpace(request.ReservationRef))
            {
                return CreditLedgerWriteResult.Fail("reservation_ref_required");
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

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => ReleaseHeldLockedAsync(request, cancellationToken),
                cancellationToken
            );
        }

        public async Task<CreditLedgerDrainTopupResult> DrainUnusedTopupAsync(
            CreditLedgerDrainTopupRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(request.SourcePaymentRef))
            {
                return CreditLedgerDrainTopupResult.Fail("source_payment_ref_required");
            }

            if (
                request.CorrectionSource is not (
                    CorrectionSources.Dispute
                    or CorrectionSources.PaymentRefund
                )
            )
            {
                return CreditLedgerDrainTopupResult.Fail("invalid_correction_source");
            }

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => DrainUnusedTopupLockedAsync(request, cancellationToken),
                cancellationToken
            );
        }

        public async Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
            CreditLedgerRestoreTopupRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(request.SourcePaymentRef))
            {
                return CreditLedgerRestoreTopupResult.Fail("source_payment_ref_required");
            }

            return await WithAccountLockAsync(
                request.RestaurantId,
                () => RestoreUnusedTopupLockedAsync(request, cancellationToken),
                cancellationToken
            );
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

        public async Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            if (!_context.Database.IsSqlServer())
            {
                var gate = AccountLocks.GetOrAdd(
                    restaurantId,
                    _ => new SemaphoreSlim(1, 1)
                );
                await gate.WaitAsync(cancellationToken);
                try
                {
                    return await MintPilotAtActivationLockedAsync(
                        restaurantId,
                        cancellationToken
                    );
                }
                finally
                {
                    gate.Release();
                }
            }

            return await MintPilotAtActivationLockedAsync(
                restaurantId,
                cancellationToken
            );
        }

        private async Task<CreditLedgerWriteResult> MintPilotAtActivationLockedAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var locked = await LockBillingAccountAsync(
                restaurantId,
                cancellationToken
            );
            if (!locked)
            {
                return CreditLedgerWriteResult.Fail("billing_account_missing");
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );

            var pricebook = _pricebookCatalog.GetRequired(
                billingAccount.ContractedPricebookId
            );
            if (!pricebook.Plans.TryGetValue("pilot", out var pilotPlan))
            {
                return CreditLedgerWriteResult.Fail("pilot_pricebook_missing");
            }

            var credits = pilotPlan.CreditsOneTime;
            if (credits == null)
            {
                return CreditLedgerWriteResult.Fail("pilot_pricebook_missing");
            }

            var existingChannels = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                )
                .Select(row => row.Channel)
                .ToListAsync(cancellationToken);

            if (existingChannels.Count > 0)
            {
                return CreditLedgerWriteResult.Fail("pilot_already_minted");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var channelGrants = new (string Channel, int Quantity)[]
            {
                (CreditChannels.Ai, credits.Ai),
                (CreditChannels.Email, credits.Email),
                (CreditChannels.Sms, credits.Sms),
            };

            var inserted = new List<CreditLedgerEntry>();
            foreach (var (channel, quantity) in channelGrants)
            {
                if (quantity <= 0)
                {
                    continue;
                }

                var row = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    Channel = channel,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = quantity,
                    AllocationId = null,
                    ReservationRef = null,
                    LocationId = null,
                    PricebookVersion = pricebook.Id,
                    ExpiresAtUtc = null,
                    PeriodStartUtc = null,
                    CreatedAtUtc = now,
                };
                inserted.Add(row);
                _context.CreditLedgerEntries.Add(row);
            }

            return CreditLedgerWriteResult.Ok(
                inserted.Select(row => new CreditLedgerInsertedRow
                {
                    Id = row.Id,
                    AllocationId = row.Id,
                    EntryType = row.EntryType,
                    Quantity = row.Quantity,
                    ReservationRef = row.ReservationRef,
                }).ToList()
            );
        }

        private async Task<CreditLedgerWriteResult> ReserveLockedAsync(
            CreditLedgerReserveRequest request,
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
            var entries = await LoadRestaurantEntriesAsync(
                request.RestaurantId,
                request.Channel,
                cancellationToken
            );

            var states = CreditLedgerCalculator.Project(entries, now);
            var poolAvailable = CreditLedgerCalculator.PoolAvailable(states);
            if (poolAvailable <= 0)
            {
                return await AbortAsync(
                    transaction,
                    "channel_hard_stopped",
                    cancellationToken
                );
            }

            if (poolAvailable < request.Units)
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

            var reservationRef = Guid.NewGuid().ToString("D");
            var inserted = new List<CreditLedgerEntry>(fills.Count);
            foreach (var fill in fills)
            {
                var row = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = request.RestaurantId,
                    Channel = request.Channel,
                    EntryType = CreditLedgerEntryTypes.Reservation,
                    Quantity = fill.Quantity,
                    AllocationId = fill.AllocationId,
                    ReservationRef = reservationRef,
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
                }).ToList(),
                reservationRef: reservationRef
            );
        }

        private async Task<CreditLedgerWriteResult> SettleLockedAsync(
            CreditLedgerSettleRequest request,
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
                    "reservation_closed",
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
            var entries = await LoadRestaurantEntriesAsync(
                request.RestaurantId,
                request.Channel,
                cancellationToken
            );

            var slices = CreditLedgerCalculator.ReservationSlices(
                entries,
                request.ReservationRef
            );
            if (
                slices.Count == 0
                || CreditLedgerCalculator.IsReservationClosed(slices)
            )
            {
                return await AbortAsync(
                    transaction,
                    "reservation_closed",
                    cancellationToken
                );
            }

            var remainingHold = CreditLedgerCalculator.TotalRemainingHold(slices);
            var settleUnits = Math.Min(request.AcceptedUnits, remainingHold);
            if (settleUnits <= 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return CreditLedgerWriteResult.Ok([]);
            }

            var inserted = new List<CreditLedgerEntry>();
            var consumedFromDraining =
                new List<CreditLedgerConsumedFromDrainingPayment>();
            var need = settleUnits;
            foreach (var slice in slices.Where(row => row.RemainingHold > 0))
            {
                var take = Math.Min(need, slice.RemainingHold);
                if (take <= 0)
                {
                    continue;
                }

                var grant = entries.First(row => row.Id == slice.AllocationId);
                if (
                    !string.IsNullOrEmpty(grant.SourcePaymentRef)
                    && CreditLedgerCalculator.IsDrainingAllocation(
                        slice.AllocationId,
                        entries
                    )
                )
                {
                    consumedFromDraining.Add(
                        new CreditLedgerConsumedFromDrainingPayment
                        {
                            SourcePaymentRef = grant.SourcePaymentRef!,
                            Channel = request.Channel,
                            Quantity = take,
                        }
                    );
                }

                inserted.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = request.RestaurantId,
                        Channel = request.Channel,
                        EntryType = CreditLedgerEntryTypes.Consumption,
                        Quantity = take,
                        AllocationId = slice.AllocationId,
                        ReservationRef = request.ReservationRef,
                        LocationId = request.LocationId,
                        CreatedAtUtc = now,
                    }
                );
                need -= take;
                if (need == 0)
                {
                    break;
                }
            }

            if (need != 0)
            {
                return await AbortAsync(
                    transaction,
                    "reservation_closed",
                    cancellationToken
                );
            }

            foreach (var row in inserted)
            {
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
                }).ToList(),
                consumedFromDraining,
                settledUnits: settleUnits
            );
        }

        private async Task<CreditLedgerWriteResult> ReleaseLockedAsync(
            CreditLedgerReleaseRequest request,
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
                    "location_not_in_account",
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
            var entries = await LoadRestaurantEntriesAsync(
                request.RestaurantId,
                request.Channel,
                cancellationToken
            );

            var slices = CreditLedgerCalculator.ReservationSlices(
                entries,
                request.ReservationRef
            );
            if (
                slices.Count == 0
                || CreditLedgerCalculator.IsReservationClosed(slices)
            )
            {
                await transaction.CommitAsync(cancellationToken);
                return CreditLedgerWriteResult.Ok([]);
            }

            var inserted = new List<CreditLedgerEntry>();
            var releasedUnits = 0;
            foreach (var slice in slices.Where(row => row.RemainingHold > 0))
            {
                var grant = entries.FirstOrDefault(row => row.Id == slice.AllocationId);
                if (grant == null || !CreditLedgerCalculator.IsGrant(grant))
                {
                    foreach (var row in inserted)
                    {
                        _context.CreditLedgerEntries.Remove(row);
                    }

                    return await AbortAsync(
                        transaction,
                        "allocation_not_found",
                        cancellationToken
                    );
                }

                var release = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = request.RestaurantId,
                    Channel = request.Channel,
                    EntryType = CreditLedgerEntryTypes.Release,
                    Quantity = slice.RemainingHold,
                    AllocationId = slice.AllocationId,
                    ReservationRef = request.ReservationRef,
                    LocationId = request.LocationId,
                    CreatedAtUtc = now,
                };
                inserted.Add(release);
                _context.CreditLedgerEntries.Add(release);
                releasedUnits += slice.RemainingHold;

                var isDraining = CreditLedgerCalculator.IsDrainingAllocation(
                    slice.AllocationId,
                    entries
                );
                var expired =
                    grant.ExpiresAtUtc != null && grant.ExpiresAtUtc.Value <= now;

                if (isDraining)
                {
                    var paymentRef = grant.SourcePaymentRef;
                    if (string.IsNullOrEmpty(paymentRef))
                    {
                        foreach (var row in inserted)
                        {
                            _context.CreditLedgerEntries.Remove(row);
                        }

                        return await AbortAsync(
                            transaction,
                            "source_payment_ref_required",
                            cancellationToken
                        );
                    }

                    var correctionSource = CreditLedgerCalculator
                        .UnrevertedPaymentRefunds(entries, paymentRef)
                        .Select(row => row.CorrectionSource)
                        .FirstOrDefault(source => !string.IsNullOrEmpty(source))
                        ?? CorrectionSources.PaymentRefund;

                    var refund = new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = request.RestaurantId,
                        Channel = request.Channel,
                        EntryType = CreditLedgerEntryTypes.Refund,
                        Quantity = slice.RemainingHold,
                        AllocationId = slice.AllocationId,
                        SourcePaymentRef = paymentRef,
                        CorrectionSource = correctionSource,
                        CreatedAtUtc = now,
                    };
                    inserted.Add(refund);
                    _context.CreditLedgerEntries.Add(refund);

                    BillingActivityWriter.TryAppend(
                        _context,
                        new BillingActivityAppendRequest
                        {
                            RestaurantId = request.RestaurantId,
                            Kind = BillingActivityKinds.TopupRefunded,
                            OccurredAtUtc = now,
                            Channel = request.Channel,
                            Qty = slice.RemainingHold,
                        }
                    );
                }
                else if (expired)
                {
                    var expiry = new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = request.RestaurantId,
                        Channel = request.Channel,
                        EntryType = CreditLedgerEntryTypes.Expiry,
                        Quantity = slice.RemainingHold,
                        AllocationId = slice.AllocationId,
                        CreatedAtUtc = now,
                    };
                    inserted.Add(expiry);
                    _context.CreditLedgerEntries.Add(expiry);
                }
            }

            if (inserted.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return CreditLedgerWriteResult.Ok([]);
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
                    AllocationId = row.AllocationId ?? row.Id,
                    EntryType = row.EntryType,
                    Quantity = row.Quantity,
                    ReservationRef = row.ReservationRef,
                }).ToList(),
                releasedUnits: releasedUnits
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
            var entries = await LoadRestaurantEntriesAsync(
                request.RestaurantId,
                request.Channel,
                cancellationToken
            );

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
            var consumedFromDraining =
                new List<CreditLedgerConsumedFromDrainingPayment>();
            foreach (var fill in fills)
            {
                var grant = entries.First(row => row.Id == fill.AllocationId);
                if (
                    !string.IsNullOrEmpty(grant.SourcePaymentRef)
                    && CreditLedgerCalculator.IsDrainingAllocation(
                        fill.AllocationId,
                        entries
                    )
                )
                {
                    consumedFromDraining.Add(
                        new CreditLedgerConsumedFromDrainingPayment
                        {
                            SourcePaymentRef = grant.SourcePaymentRef!,
                            Channel = request.Channel,
                            Quantity = fill.Quantity,
                        }
                    );
                }

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
                }).ToList(),
                consumedFromDraining
            );
        }

        private async Task<CreditLedgerMintTopupResult> MintTopupLockedAsync(
            CreditLedgerMintTopupRequest request,
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
                return CreditLedgerMintTopupResult.Fail("restaurant_not_found");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var allocationId = Guid.NewGuid();
            var row = new CreditLedgerEntry
            {
                Id = allocationId,
                RestaurantId = request.RestaurantId,
                Channel = request.Channel,
                EntryType = CreditLedgerEntryTypes.TopupAllocation,
                Quantity = request.Quantity,
                PricebookVersion = _pricebookCatalog.CurrentPricebookId,
                SourcePaymentRef = request.SourcePaymentRef,
                ExpiresAtUtc = now.AddMonths(12),
                CreatedAtUtc = now,
            };
            _context.CreditLedgerEntries.Add(row);

            var postState = CreditLedgerCalculator.Project([row], now);
            if (!CreditLedgerCalculator.InvariantsHold(postState))
            {
                _context.CreditLedgerEntries.Remove(row);
                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerMintTopupResult.Fail("invalid_grant");
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CreditLedgerMintTopupResult.Ok(allocationId);
        }

        private async Task<CreditLedgerDrainTopupResult> DrainUnusedTopupLockedAsync(
            CreditLedgerDrainTopupRequest request,
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
                return CreditLedgerDrainTopupResult.Fail("restaurant_not_found");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var entries = await LoadRestaurantEntriesAsync(
                request.RestaurantId,
                cancellationToken: cancellationToken
            );
            var grants = CreditLedgerCalculator.TopupGrantsForPaymentRef(
                entries,
                request.SourcePaymentRef
            );
            if (grants.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerDrainTopupResult.Fail("source_payment_ref_not_found");
            }

            var inserted = DrainBindableRefunds(
                request.RestaurantId,
                request.SourcePaymentRef,
                request.CorrectionSource,
                entries,
                now
            );
            foreach (var row in inserted)
            {
                _context.CreditLedgerEntries.Add(row);
            }

            var working = entries.Concat(inserted).ToList();
            var postState = CreditLedgerCalculator.Project(working, now);
            if (!CreditLedgerCalculator.InvariantsHold(postState))
            {
                foreach (var row in inserted)
                {
                    _context.CreditLedgerEntries.Remove(row);
                }

                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerDrainTopupResult.Fail("negative_remaining_refused");
            }

            var refundedThisCommit = inserted
                .GroupBy(row => row.Channel)
                .ToDictionary(group => group.Key, group => group.Sum(row => row.Quantity));
            foreach (var (channel, qty) in refundedThisCommit.Where(pair => pair.Value > 0))
            {
                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = request.RestaurantId,
                        Kind = BillingActivityKinds.TopupRefunded,
                        OccurredAtUtc = now,
                        Channel = channel,
                        Qty = qty,
                    }
                );
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var channels = CreditLedgerCalculator
                .SummarizePaymentRef(
                    working,
                    request.SourcePaymentRef,
                    now,
                    refundedThisCommit
                )
                .Select(row => new TopupPaymentChannelSnapshot
                {
                    Channel = row.Channel,
                    Refunded = row.Refunded,
                    Held = row.Held,
                    Consumed = row.Consumed,
                })
                .ToList();

            return CreditLedgerDrainTopupResult.Ok(channels);
        }

        private async Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupLockedAsync(
            CreditLedgerRestoreTopupRequest request,
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
                return CreditLedgerRestoreTopupResult.Fail("restaurant_not_found");
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var entries = await LoadRestaurantEntriesAsync(
                request.RestaurantId,
                cancellationToken: cancellationToken
            );
            var grants = CreditLedgerCalculator.TopupGrantsForPaymentRef(
                entries,
                request.SourcePaymentRef
            );
            if (grants.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerRestoreTopupResult.Fail("source_payment_ref_not_found");
            }

            var refunds = CreditLedgerCalculator.UnrevertedPaymentRefunds(
                entries,
                request.SourcePaymentRef
            );
            var inserted = new List<CreditLedgerEntry>(refunds.Count);
            foreach (var refund in refunds)
            {
                var reversal = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = request.RestaurantId,
                    Channel = refund.Channel,
                    EntryType = CreditLedgerEntryTypes.Reversal,
                    Quantity = refund.Quantity,
                    AllocationId = refund.AllocationId,
                    ReversedEntryId = refund.Id,
                    CreatedAtUtc = now,
                };
                inserted.Add(reversal);
                _context.CreditLedgerEntries.Add(reversal);
            }

            var working = entries.Concat(inserted).ToList();
            var postState = CreditLedgerCalculator.Project(working, now);
            if (!CreditLedgerCalculator.InvariantsHold(postState))
            {
                foreach (var row in inserted)
                {
                    _context.CreditLedgerEntries.Remove(row);
                }

                await transaction.RollbackAsync(cancellationToken);
                return CreditLedgerRestoreTopupResult.Fail("negative_remaining_refused");
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var channels = CreditLedgerCalculator
                .SummarizePaymentRef(working, request.SourcePaymentRef, now)
                .Select(row => new TopupPaymentChannelSnapshot
                {
                    Channel = row.Channel,
                    Refunded = row.Refunded,
                    Held = row.Held,
                    Consumed = row.Consumed,
                })
                .ToList();

            return CreditLedgerRestoreTopupResult.Ok(channels);
        }

        private async Task<CreditLedgerWriteResult> ReleaseHeldLockedAsync(
            CreditLedgerReleaseHeldRequest request,
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
                    "restaurant_not_found",
                    cancellationToken
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var entries = await LoadRestaurantEntriesAsync(
                request.RestaurantId,
                request.Channel,
                cancellationToken
            );
            var grant = entries.FirstOrDefault(row => row.Id == request.AllocationId);
            if (grant == null || !CreditLedgerCalculator.IsGrant(grant))
            {
                return await AbortAsync(
                    transaction,
                    "allocation_not_found",
                    cancellationToken
                );
            }

            var reserved = entries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.Reservation
                    && row.AllocationId == request.AllocationId
                    && row.ReservationRef == request.ReservationRef
                )
                .Sum(row => row.Quantity);
            var closed = entries
                .Where(row =>
                    row.AllocationId == request.AllocationId
                    && row.ReservationRef == request.ReservationRef
                    && (
                        row.EntryType == CreditLedgerEntryTypes.Release
                        || row.EntryType == CreditLedgerEntryTypes.Consumption
                    )
                )
                .Sum(row => row.Quantity);
            var openHeld = reserved - closed;
            if (openHeld < request.Quantity)
            {
                return await AbortAsync(
                    transaction,
                    "insufficient_held",
                    cancellationToken
                );
            }

            var inserted = new List<CreditLedgerEntry>();
            var release = new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                RestaurantId = request.RestaurantId,
                Channel = request.Channel,
                EntryType = CreditLedgerEntryTypes.Release,
                Quantity = request.Quantity,
                AllocationId = request.AllocationId,
                ReservationRef = request.ReservationRef,
                LocationId = request.LocationId,
                CreatedAtUtc = now,
            };
            inserted.Add(release);
            _context.CreditLedgerEntries.Add(release);

            var isDraining = CreditLedgerCalculator.IsDrainingAllocation(
                request.AllocationId,
                entries
            );
            var expired =
                grant.ExpiresAtUtc != null && grant.ExpiresAtUtc.Value <= now;

            if (isDraining)
            {
                var paymentRef = grant.SourcePaymentRef;
                if (string.IsNullOrEmpty(paymentRef))
                {
                    foreach (var row in inserted)
                    {
                        _context.CreditLedgerEntries.Remove(row);
                    }

                    return await AbortAsync(
                        transaction,
                        "source_payment_ref_required",
                        cancellationToken
                    );
                }

                var correctionSource = CreditLedgerCalculator
                    .UnrevertedPaymentRefunds(entries, paymentRef)
                    .Select(row => row.CorrectionSource)
                    .FirstOrDefault(source => !string.IsNullOrEmpty(source))
                    ?? CorrectionSources.PaymentRefund;

                var refund = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = request.RestaurantId,
                    Channel = request.Channel,
                    EntryType = CreditLedgerEntryTypes.Refund,
                    Quantity = request.Quantity,
                    AllocationId = request.AllocationId,
                    SourcePaymentRef = paymentRef,
                    CorrectionSource = correctionSource,
                    CreatedAtUtc = now,
                };
                inserted.Add(refund);
                _context.CreditLedgerEntries.Add(refund);

                BillingActivityWriter.TryAppend(
                    _context,
                    new BillingActivityAppendRequest
                    {
                        RestaurantId = request.RestaurantId,
                        Kind = BillingActivityKinds.TopupRefunded,
                        OccurredAtUtc = now,
                        Channel = request.Channel,
                        Qty = request.Quantity,
                    }
                );
            }
            else if (expired)
            {
                var expiry = new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = request.RestaurantId,
                    Channel = request.Channel,
                    EntryType = CreditLedgerEntryTypes.Expiry,
                    Quantity = request.Quantity,
                    AllocationId = request.AllocationId,
                    CreatedAtUtc = now,
                };
                inserted.Add(expiry);
                _context.CreditLedgerEntries.Add(expiry);
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
                    AllocationId = row.AllocationId ?? row.Id,
                    EntryType = row.EntryType,
                    Quantity = row.Quantity,
                    ReservationRef = row.ReservationRef,
                }).ToList()
            );
        }

        private static List<CreditLedgerEntry> DrainBindableRefunds(
            int restaurantId,
            string sourcePaymentRef,
            string correctionSource,
            List<CreditLedgerEntry> entries,
            DateTime nowUtc
        )
        {
            var inserted = new List<CreditLedgerEntry>();
            var working = entries;

            while (true)
            {
                var states = CreditLedgerCalculator.Project(working, nowUtc);
                var byId = states.ToDictionary(row => row.Id);
                var batch = new List<CreditLedgerEntry>();

                foreach (var grant in CreditLedgerCalculator.TopupGrantsForPaymentRef(
                    working,
                    sourcePaymentRef
                ))
                {
                    if (!byId.TryGetValue(grant.Id, out var state) || state.Bindable <= 0)
                    {
                        continue;
                    }

                    batch.Add(
                        new CreditLedgerEntry
                        {
                            Id = Guid.NewGuid(),
                            RestaurantId = restaurantId,
                            Channel = grant.Channel,
                            EntryType = CreditLedgerEntryTypes.Refund,
                            Quantity = state.Bindable,
                            AllocationId = grant.Id,
                            SourcePaymentRef = sourcePaymentRef,
                            CorrectionSource = correctionSource,
                            CreatedAtUtc = nowUtc,
                        }
                    );
                }

                if (batch.Count == 0)
                {
                    break;
                }

                inserted.AddRange(batch);
                working = [.. working, .. batch];
            }

            return inserted;
        }

        private async Task<List<CreditLedgerEntry>> LoadRestaurantEntriesAsync(
            int restaurantId,
            string? channel = null,
            CancellationToken cancellationToken = default
        )
        {
            var query = _context.CreditLedgerEntries.Where(row =>
                row.RestaurantId == restaurantId
            );
            if (channel != null)
            {
                query = query.Where(row => row.Channel == channel);
            }

            return await query.ToListAsync(cancellationToken);
        }

        private async Task<T> WithAccountLockAsync<T>(
            int restaurantId,
            Func<Task<T>> action,
            CancellationToken cancellationToken
        )
        {
            if (!_context.Database.IsSqlServer())
            {
                var gate = AccountLocks.GetOrAdd(
                    restaurantId,
                    _ => new SemaphoreSlim(1, 1)
                );
                await gate.WaitAsync(cancellationToken);
                try
                {
                    return await action();
                }
                finally
                {
                    gate.Release();
                }
            }

            return await action();
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

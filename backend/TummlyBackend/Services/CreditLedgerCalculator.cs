using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    internal readonly record struct CreditAllocationState(
        Guid Id,
        string EntryType,
        DateTime CreatedAtUtc,
        DateTime? ExpiresAtUtc,
        DateTime? PeriodStartUtc,
        int GrantQuantity,
        int Remaining,
        int Held,
        int Bindable
    );

    internal static class CreditLedgerCalculator
    {
        public static IReadOnlyList<CreditAllocationState> Project(
            IReadOnlyList<CreditLedgerEntry> entries,
            DateTime nowUtc
        )
        {
            var grants = entries.Where(IsGrant).ToList();
            var byId = entries.ToDictionary(row => row.Id);
            var states = new List<CreditAllocationState>(grants.Count);

            foreach (var grant in grants)
            {
                var remainingDebit = 0;
                var heldOpen = 0;
                var heldClosed = 0;

                foreach (var row in entries)
                {
                    if (row.AllocationId != grant.Id)
                    {
                        continue;
                    }

                    if (IsRemainingDebit(row))
                    {
                        remainingDebit += row.Quantity;
                    }
                    else if (row.EntryType == CreditLedgerEntryTypes.Reversal)
                    {
                        if (
                            row.ReversedEntryId is Guid reversedId
                            && byId.TryGetValue(reversedId, out var reversed)
                            && IsRemainingDebit(reversed)
                        )
                        {
                            remainingDebit -= row.Quantity;
                        }
                    }
                    else if (row.EntryType == CreditLedgerEntryTypes.Reservation)
                    {
                        heldOpen += row.Quantity;
                    }
                    else if (row.EntryType == CreditLedgerEntryTypes.Release)
                    {
                        heldClosed += row.Quantity;
                    }

                    if (
                        row.EntryType == CreditLedgerEntryTypes.Consumption
                        && !string.IsNullOrEmpty(row.ReservationRef)
                    )
                    {
                        heldClosed += row.Quantity;
                    }
                }

                var remaining = grant.Quantity - remainingDebit;
                var held = heldOpen - heldClosed;
                var unheld = remaining - held;
                var expired =
                    grant.ExpiresAtUtc != null
                    && grant.ExpiresAtUtc.Value <= nowUtc;
                var bindable = expired ? 0 : unheld;

                states.Add(
                    new CreditAllocationState(
                        grant.Id,
                        grant.EntryType,
                        grant.CreatedAtUtc,
                        grant.ExpiresAtUtc,
                        grant.PeriodStartUtc,
                        grant.Quantity,
                        remaining,
                        held,
                        bindable
                    )
                );
            }

            return states;
        }

        public static IReadOnlyList<(Guid AllocationId, int Quantity)> Bind(
            IReadOnlyList<CreditAllocationState> states,
            int units
        )
        {
            var fills = new List<(Guid AllocationId, int Quantity)>();
            var remainingNeed = units;

            foreach (var slice in states.Where(row => row.Bindable > 0).OrderBy(BindKey))
            {
                var take = Math.Min(remainingNeed, slice.Bindable);
                fills.Add((slice.Id, take));
                remainingNeed -= take;
                if (remainingNeed == 0)
                {
                    break;
                }
            }

            if (remainingNeed != 0)
            {
                return [];
            }

            return fills;
        }

        public static IReadOnlyList<(Guid AllocationId, int Quantity)> BindDebit(
            IReadOnlyList<CreditAllocationState> states,
            int units,
            Guid? allocationId = null
        )
        {
            if (allocationId is Guid targetId)
            {
                var slice = states.FirstOrDefault(row => row.Id == targetId);
                if (slice.Id == default || slice.Bindable < units)
                {
                    return [];
                }

                return [(targetId, units)];
            }

            var fills = new List<(Guid AllocationId, int Quantity)>();
            var remainingNeed = units;

            foreach (
                var slice in states
                    .Where(row => row.Bindable > 0)
                    .OrderByDescending(DebitRank)
                    .ThenBy(DebitExpiry)
                    .ThenBy(row => row.CreatedAtUtc)
            )
            {
                var take = Math.Min(remainingNeed, slice.Bindable);
                fills.Add((slice.Id, take));
                remainingNeed -= take;
                if (remainingNeed == 0)
                {
                    break;
                }
            }

            if (remainingNeed != 0)
            {
                return [];
            }

            return fills;
        }

        public static int PoolAvailable(IReadOnlyList<CreditAllocationState> states)
        {
            return states.Sum(row => Math.Max(row.Bindable, 0));
        }

        public static bool InvariantsHold(IReadOnlyList<CreditAllocationState> states)
        {
            foreach (var state in states)
            {
                if (state.Held < 0 || state.Remaining < state.Held)
                {
                    return false;
                }
            }

            return PoolAvailable(states) >= 0;
        }

        public static bool IsGrant(CreditLedgerEntry entry)
        {
            if (entry.EntryType == CreditLedgerEntryTypes.ManualAdjustment)
            {
                return entry.AllocationId == null;
            }

            return entry.EntryType is
                CreditLedgerEntryTypes.IncludedAllocation
                or CreditLedgerEntryTypes.PilotAllocation
                or CreditLedgerEntryTypes.TopupAllocation
                or CreditLedgerEntryTypes.PlanMigration;
        }

        public static bool IsIncludedClass(string entryType)
        {
            return entryType is
                CreditLedgerEntryTypes.IncludedAllocation
                or CreditLedgerEntryTypes.PlanMigration;
        }

        public static bool IsTopUpClass(string entryType)
        {
            return entryType is
                CreditLedgerEntryTypes.TopupAllocation
                or CreditLedgerEntryTypes.ManualAdjustment;
        }

        private static bool IsRemainingDebit(CreditLedgerEntry entry)
        {
            if (entry.EntryType == CreditLedgerEntryTypes.ManualAdjustment)
            {
                return entry.AllocationId != null;
            }

            return entry.EntryType is
                CreditLedgerEntryTypes.Consumption
                or CreditLedgerEntryTypes.Expiry
                or CreditLedgerEntryTypes.Refund;
        }

        private static (int Rank, DateTime Expiry, DateTime Created) BindKey(
            CreditAllocationState state
        )
        {
            var rank = DebitRank(state);
            var expiry = rank == 2
                ? state.ExpiresAtUtc ?? DateTime.MaxValue
                : DateTime.MinValue;
            return (rank, expiry, state.CreatedAtUtc);
        }

        private static int DebitRank(CreditAllocationState state)
        {
            return IsIncludedClass(state.EntryType)
                ? 0
                : state.EntryType == CreditLedgerEntryTypes.PilotAllocation
                    ? 1
                    : 2;
        }

        private static DateTime DebitExpiry(CreditAllocationState state)
        {
            return DebitRank(state) == 2
                ? state.ExpiresAtUtc ?? DateTime.MaxValue
                : DateTime.MinValue;
        }
    }
}

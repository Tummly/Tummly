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

    internal readonly record struct CreditReservationSlice(
        Guid AllocationId,
        string EntryType,
        DateTime CreatedAtUtc,
        DateTime? ExpiresAtUtc,
        int Reserved,
        int Consumed,
        int Released,
        int RemainingHold
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

        public static IReadOnlyList<CreditReservationSlice> ReservationSlices(
            IReadOnlyList<CreditLedgerEntry> entries,
            string reservationRef
        )
        {
            if (string.IsNullOrWhiteSpace(reservationRef))
            {
                return [];
            }

            var reservations = entries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.Reservation
                    && row.ReservationRef == reservationRef
                    && row.AllocationId != null
                )
                .ToList();
            if (reservations.Count == 0)
            {
                return [];
            }

            var grants = entries.Where(IsGrant).ToDictionary(row => row.Id);
            var slices = new List<CreditReservationSlice>(reservations.Count);

            foreach (var group in reservations.GroupBy(row => row.AllocationId!.Value))
            {
                if (!grants.TryGetValue(group.Key, out var grant))
                {
                    continue;
                }

                var reserved = group.Sum(row => row.Quantity);
                var consumed = entries
                    .Where(row =>
                        row.EntryType == CreditLedgerEntryTypes.Consumption
                        && row.ReservationRef == reservationRef
                        && row.AllocationId == group.Key
                    )
                    .Sum(row => row.Quantity);
                var released = entries
                    .Where(row =>
                        row.EntryType == CreditLedgerEntryTypes.Release
                        && row.ReservationRef == reservationRef
                        && row.AllocationId == group.Key
                    )
                    .Sum(row => row.Quantity);
                var remainingHold = reserved - consumed - released;

                slices.Add(
                    new CreditReservationSlice(
                        group.Key,
                        grant.EntryType,
                        grant.CreatedAtUtc,
                        grant.ExpiresAtUtc,
                        reserved,
                        consumed,
                        released,
                        remainingHold
                    )
                );
            }

            return slices
                .OrderBy(row => BindKey(
                    new CreditAllocationState(
                        row.AllocationId,
                        row.EntryType,
                        row.CreatedAtUtc,
                        row.ExpiresAtUtc,
                        null,
                        0,
                        0,
                        0,
                        0
                    )
                ))
                .ToList();
        }

        public static bool IsReservationClosed(
            IReadOnlyList<CreditReservationSlice> slices
        )
        {
            return slices.Count == 0
                || slices.All(row => row.RemainingHold <= 0);
        }

        public static int TotalRemainingHold(
            IReadOnlyList<CreditReservationSlice> slices
        )
        {
            return slices.Sum(row => Math.Max(row.RemainingHold, 0));
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

        public static bool IsReversed(
            Guid entryId,
            IReadOnlyList<CreditLedgerEntry> entries
        )
        {
            return entries.Any(row =>
                row.EntryType == CreditLedgerEntryTypes.Reversal
                && row.ReversedEntryId == entryId
            );
        }

        public static bool IsDrainingAllocation(
            Guid allocationId,
            IReadOnlyList<CreditLedgerEntry> entries
        )
        {
            return entries.Any(row =>
                row.AllocationId == allocationId
                && row.EntryType == CreditLedgerEntryTypes.Refund
                && !string.IsNullOrEmpty(row.SourcePaymentRef)
                && !IsReversed(row.Id, entries)
            );
        }

        public static IReadOnlyList<CreditLedgerEntry> TopupGrantsForPaymentRef(
            IReadOnlyList<CreditLedgerEntry> entries,
            string sourcePaymentRef
        )
        {
            return entries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                    && row.SourcePaymentRef == sourcePaymentRef
                )
                .ToList();
        }

        public static IReadOnlyList<TopupPaymentChannelTotals> SummarizePaymentRef(
            IReadOnlyList<CreditLedgerEntry> entries,
            string sourcePaymentRef,
            DateTime nowUtc,
            IReadOnlyDictionary<string, int>? refundedThisCommit = null
        )
        {
            var grants = TopupGrantsForPaymentRef(entries, sourcePaymentRef);
            if (grants.Count == 0)
            {
                return [];
            }

            var states = Project(entries, nowUtc);
            var byId = states.ToDictionary(row => row.Id);

            return grants
                .GroupBy(row => row.Channel)
                .Select(group =>
                {
                    var held = group.Sum(grant =>
                        byId.TryGetValue(grant.Id, out var state) ? state.Held : 0
                    );
                    var consumed = group.Sum(grant =>
                        entries
                            .Where(row =>
                                row.AllocationId == grant.Id
                                && row.EntryType == CreditLedgerEntryTypes.Consumption
                            )
                            .Sum(row => row.Quantity)
                    );
                    var refunded = refundedThisCommit?.GetValueOrDefault(group.Key) ?? 0;
                    return new TopupPaymentChannelTotals(
                        group.Key,
                        refunded,
                        held,
                        consumed
                    );
                })
                .OrderBy(row => row.Channel)
                .ToList();
        }

        public static IReadOnlyList<CreditLedgerEntry> UnrevertedPaymentRefunds(
            IReadOnlyList<CreditLedgerEntry> entries,
            string sourcePaymentRef
        )
        {
            return entries
                .Where(row =>
                    row.EntryType == CreditLedgerEntryTypes.Refund
                    && row.SourcePaymentRef == sourcePaymentRef
                    && !IsReversed(row.Id, entries)
                )
                .ToList();
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

    internal readonly record struct TopupPaymentChannelTotals(
        string Channel,
        int Refunded,
        int Held,
        int Consumed
    );
}

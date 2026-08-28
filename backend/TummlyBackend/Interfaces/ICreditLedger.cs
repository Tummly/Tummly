namespace TummlyBackend.Interfaces
{
    public interface ICreditLedger
    {
        Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
            CreditLedgerConsumeRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CreditLedgerWriteResult> StaffManualAdjustAsync(
            StaffManualAdjustRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CreditLedgerWriteResult> StaffReverseAsync(
            StaffReverseRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
            CreditLedgerMintTopupRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CreditLedgerDrainTopupResult> DrainUnusedTopupAsync(
            CreditLedgerDrainTopupRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
            CreditLedgerRestoreTopupRequest request,
            CancellationToken cancellationToken = default
        );

        Task<CreditLedgerWriteResult> ReleaseHeldAsync(
            CreditLedgerReleaseHeldRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class StaffManualAdjustRequest
    {
        public int RestaurantId { get; init; }

        public string Channel { get; init; } = string.Empty;

        public string Direction { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public string Reason { get; init; } = string.Empty;

        public int ActorStaffUserId { get; init; }

        public Guid? AllocationId { get; init; }

        public int? HelpCentreQueryId { get; init; }
    }

    public sealed class StaffReverseRequest
    {
        public Guid ReversedEntryId { get; init; }

        public string Reason { get; init; } = string.Empty;

        public int ActorStaffUserId { get; init; }

        public int? HelpCentreQueryId { get; init; }
    }

    public sealed class CreditLedgerConsumeRequest
    {
        public int RestaurantId { get; init; }

        public string Channel { get; init; } = string.Empty;

        public int Units { get; init; }

        public int? LocationId { get; init; }
    }

    public sealed class CreditLedgerMintTopupRequest
    {
        public int RestaurantId { get; init; }

        public string Channel { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public string SourcePaymentRef { get; init; } = string.Empty;
    }

    public sealed class CreditLedgerDrainTopupRequest
    {
        public int RestaurantId { get; init; }

        public string SourcePaymentRef { get; init; } = string.Empty;

        public string CorrectionSource { get; init; } = string.Empty;
    }

    public sealed class CreditLedgerRestoreTopupRequest
    {
        public int RestaurantId { get; init; }

        public string SourcePaymentRef { get; init; } = string.Empty;
    }

    public sealed class CreditLedgerReleaseHeldRequest
    {
        public int RestaurantId { get; init; }

        public string Channel { get; init; } = string.Empty;

        public Guid AllocationId { get; init; }

        public string ReservationRef { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public int? LocationId { get; init; }
    }

    public sealed class CreditLedgerWriteResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public IReadOnlyList<CreditLedgerInsertedRow> Inserted { get; init; }
            = [];

        public IReadOnlyList<CreditLedgerConsumedFromDrainingPayment> ConsumedFromDrainingPayment
        { get; init; } = [];

        public static CreditLedgerWriteResult Ok(
            IReadOnlyList<CreditLedgerInsertedRow> inserted,
            IReadOnlyList<CreditLedgerConsumedFromDrainingPayment>? consumedFromDraining = null
        )
        {
            return new CreditLedgerWriteResult
            {
                Succeeded = true,
                Inserted = inserted,
                ConsumedFromDrainingPayment =
                    consumedFromDraining ?? [],
            };
        }

        public static CreditLedgerWriteResult Fail(string code)
        {
            return new CreditLedgerWriteResult
            {
                Succeeded = false,
                Code = code,
            };
        }
    }

    public sealed class CreditLedgerMintTopupResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public Guid? AllocationId { get; init; }

        public static CreditLedgerMintTopupResult Ok(Guid allocationId)
        {
            return new CreditLedgerMintTopupResult
            {
                Succeeded = true,
                AllocationId = allocationId,
            };
        }

        public static CreditLedgerMintTopupResult Fail(string code)
        {
            return new CreditLedgerMintTopupResult
            {
                Succeeded = false,
                Code = code,
            };
        }
    }

    public sealed class TopupPaymentChannelSnapshot
    {
        public string Channel { get; init; } = string.Empty;

        public int Refunded { get; init; }

        public int Held { get; init; }

        public int Consumed { get; init; }
    }

    public sealed class CreditLedgerDrainTopupResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public IReadOnlyList<TopupPaymentChannelSnapshot> Channels { get; init; } = [];

        public static CreditLedgerDrainTopupResult Ok(
            IReadOnlyList<TopupPaymentChannelSnapshot> channels
        )
        {
            return new CreditLedgerDrainTopupResult
            {
                Succeeded = true,
                Channels = channels,
            };
        }

        public static CreditLedgerDrainTopupResult Fail(string code)
        {
            return new CreditLedgerDrainTopupResult
            {
                Succeeded = false,
                Code = code,
            };
        }
    }

    public sealed class CreditLedgerRestoreTopupResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public IReadOnlyList<TopupPaymentChannelSnapshot> Channels { get; init; } = [];

        public static CreditLedgerRestoreTopupResult Ok(
            IReadOnlyList<TopupPaymentChannelSnapshot> channels
        )
        {
            return new CreditLedgerRestoreTopupResult
            {
                Succeeded = true,
                Channels = channels,
            };
        }

        public static CreditLedgerRestoreTopupResult Fail(string code)
        {
            return new CreditLedgerRestoreTopupResult
            {
                Succeeded = false,
                Code = code,
            };
        }
    }

    public sealed class CreditLedgerInsertedRow
    {
        public Guid Id { get; init; }

        public Guid AllocationId { get; init; }

        public string EntryType { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public string? ReservationRef { get; init; }
    }

    public sealed class CreditLedgerConsumedFromDrainingPayment
    {
        public string SourcePaymentRef { get; init; } = string.Empty;

        public string Channel { get; init; } = string.Empty;

        public int Quantity { get; init; }
    }
}

namespace TummlyBackend.Interfaces
{
    public interface ICreditLedger
    {
        Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
            CreditLedgerConsumeRequest request,
            CancellationToken cancellationToken = default
        );
    }

    public sealed class CreditLedgerConsumeRequest
    {
        public int RestaurantId { get; init; }

        public string Channel { get; init; } = string.Empty;

        public int Units { get; init; }

        public int? LocationId { get; init; }
    }

    public sealed class CreditLedgerWriteResult
    {
        public bool Succeeded { get; init; }

        public string? Code { get; init; }

        public IReadOnlyList<CreditLedgerInsertedRow> Inserted { get; init; }
            = [];

        public static CreditLedgerWriteResult Ok(
            IReadOnlyList<CreditLedgerInsertedRow> inserted
        )
        {
            return new CreditLedgerWriteResult
            {
                Succeeded = true,
                Inserted = inserted,
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

    public sealed class CreditLedgerInsertedRow
    {
        public Guid Id { get; init; }

        public Guid AllocationId { get; init; }

        public string EntryType { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public string? ReservationRef { get; init; }
    }
}

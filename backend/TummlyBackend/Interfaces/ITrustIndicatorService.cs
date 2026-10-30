using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface ITrustIndicatorService
    {
        Task<AdminTrustIndicatorsViewDto> GetAdminViewAsync(
            int actorAdminUserId,
            CancellationToken cancellationToken = default
        );

        Task<TrustIndicatorOutcome<TrustIndicatorItemDto>> UpdateDraftItemAsync(
            int actorAdminUserId,
            int itemNumber,
            TrustIndicatorEditableValues request,
            CancellationToken cancellationToken = default
        );

        Task<TrustIndicatorOutcome<TrustIndicatorReviewDto>> SubmitDraftAsync(
            int actorAdminUserId,
            CancellationToken cancellationToken = default
        );

        Task<TrustIndicatorOutcome<TrustIndicatorPublicationDto>> PublishAsync(
            int actorAdminUserId,
            string? idempotencyKey,
            string? reason,
            string? draftRevision,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<PublicTrustIndicatorDto>> GetPublicAsync(
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>Result of a trust indicator write. Code is null on success.</summary>
    public sealed record TrustIndicatorOutcome<T>(
        T? Value,
        string? Code = null,
        string? Message = null,
        IReadOnlyList<string>? Details = null
    )
    {
        public bool Succeeded => Code == null;

        public static TrustIndicatorOutcome<T> Ok(T value) => new(value);

        public static TrustIndicatorOutcome<T> Fail(
            string code,
            string message,
            IReadOnlyList<string>? details = null
        ) => new(default, code, message, details);
    }
}

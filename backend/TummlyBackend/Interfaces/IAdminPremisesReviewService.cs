using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminPremisesReviewService
    {
        Task<IReadOnlyList<AdminPremisesReviewItemDto>> ListPendingAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Clears or rejects a PendingReview Location. A reason is required.
        /// Writes the Location and its <c>AdminAuditEvent</c> in one save.
        /// </summary>
        Task<AdminPremisesReviewResolveResult> ResolveAsync(
            int locationId,
            bool clear,
            string? reason,
            int adminUserId,
            string adminIdentity,
            CancellationToken cancellationToken = default
        );
    }
}

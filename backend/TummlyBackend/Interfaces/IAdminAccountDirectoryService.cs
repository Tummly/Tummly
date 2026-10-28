using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminAccountDirectoryService
    {
        Task<AdminAccountListResultDto> ListAsync(
            AdminAccountListQuery query,
            CancellationToken cancellationToken = default
        );

        /// <summary>Returns null when the Workspace does not exist.</summary>
        Task<AdminAccountProfileDto?> GetProfileAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );
    }

    /// <param name="Search">Workspace ID (exact) or Workspace / trading / legal name (contains).</param>
    /// <param name="CreatedFrom">Inclusive lower bound on Workspace creation (UTC).</param>
    /// <param name="CreatedBefore">Exclusive upper bound on Workspace creation (UTC).</param>
    public sealed record AdminAccountListQuery(
        string? Search = null,
        string? SubscriptionPlan = null,
        string? BillingStatus = null,
        DateTime? CreatedFrom = null,
        DateTime? CreatedBefore = null,
        int Skip = 0,
        int Take = 50
    );
}

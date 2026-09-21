using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminAuditService
    {
        void Append(AdminAuditAppendRequest request);

        Task<AdminAuditListResult> ListAsync(
            AdminAuditListQuery query,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record AdminAuditAppendRequest(
        string Action,
        string ActorIdentity,
        string TargetType,
        string TargetId,
        int? ActorAdminUserId = null,
        int? RestaurantId = null,
        string? DetailJson = null
    );

    public sealed record AdminAuditListQuery(
        string? Action = null,
        int? RestaurantId = null,
        DateTime? FromUtc = null,
        DateTime? ToUtc = null,
        int Skip = 0,
        int Take = 50
    );

    public sealed record AdminAuditListResult(
        IReadOnlyList<AdminAuditEventDto> Items,
        int TotalCount
    );
}

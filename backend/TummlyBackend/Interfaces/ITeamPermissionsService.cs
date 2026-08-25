using TummlyBackend.DTOs.TeamPermissions;

namespace TummlyBackend.Interfaces
{
    public interface ITeamPermissionsService
    {
        Task<TeamPermissionsPageDto?> GetPageAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage
        );

        Task<AccessActivityListDto> GetAccessActivityAsync(
            int restaurantId,
            int page,
            int pageSize
        );

        Task<string?> UpdateRoleAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId,
            string permissionRole
        );

        Task<string?> UpdateLocationScopeAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId,
            IReadOnlyList<int> allowedLocationIds,
            string locationScope,
            int[] namedLocationIds
        );

        Task<string?> DeactivateAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        );

        Task<string?> ReactivateAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        );

        Task<string?> RemoveAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        );

        Task<string?> UpdateAdminMatrixAsync(
            int actorUserId,
            int restaurantId,
            IReadOnlyList<AdminMatrixCellDto> adminCells
        );

        Task<string?> SendInviteAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            IReadOnlyList<int> allowedLocationIds,
            SendTeamInvitationRequest request
        );

        Task<string?> ResendInviteAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int invitationId
        );

        Task<string?> RevokeInviteAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int invitationId
        );
    }
}

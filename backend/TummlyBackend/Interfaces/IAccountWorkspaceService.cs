using TummlyBackend.DTOs.AccountWorkspace;

namespace TummlyBackend.Interfaces
{
    public interface IAccountWorkspaceService
    {
        Task<AccountWorkspaceDetailsDto?> GetDetailsAsync(
            int actorUserId,
            int restaurantId
        );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            UpdateAccountDetailsAsync(
                int actorUserId,
                int restaurantId,
                string? name,
                IFormFile? logo
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            UpdateBusinessDetailsAsync(
                int actorUserId,
                int restaurantId,
                UpdateBusinessDetailsRequest request
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            UpdateKeyContactsAsync(
                int actorUserId,
                int restaurantId,
                UpdateKeyContactsRequest request
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            UpdateWorkspaceDefaultsAsync(
                int actorUserId,
                int restaurantId,
                UpdateWorkspaceDefaultsRequest request
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            PauseWorkspaceAsync(int actorUserId, int restaurantId);

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            ResumeWorkspaceAsync(int actorUserId, int restaurantId);

        Task<(Stream Stream, string ContentType)?> OpenBrandLogoAsync(
            int restaurantId
        );

        Task<(Stream Stream, string ContentType)?> OpenPublicBrandLogoAsync(
            string objectKey
        );
    }
}

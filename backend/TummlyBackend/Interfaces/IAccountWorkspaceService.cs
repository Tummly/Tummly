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
                int ownerUserId,
                string? name,
                IFormFile? logo
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            UpdateBusinessDetailsAsync(
                int ownerUserId,
                UpdateBusinessDetailsRequest request
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            UpdateKeyContactsAsync(
                int ownerUserId,
                UpdateKeyContactsRequest request
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            UpdateWorkspaceDefaultsAsync(
                int ownerUserId,
                UpdateWorkspaceDefaultsRequest request
            );

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            PauseWorkspaceAsync(int actorUserId);

        Task<(AccountWorkspaceDetailsDto? Details, string? Error, int StatusCode)>
            ResumeWorkspaceAsync(int actorUserId);

        Task<(Stream Stream, string ContentType)?> OpenBrandLogoAsync(
            int ownerUserId
        );

        Task<(Stream Stream, string ContentType)?> OpenPublicBrandLogoAsync(
            string objectKey
        );
    }
}

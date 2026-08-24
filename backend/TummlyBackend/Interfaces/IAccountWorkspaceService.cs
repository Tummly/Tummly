using TummlyBackend.DTOs.AccountWorkspace;

namespace TummlyBackend.Interfaces
{
    public interface IAccountWorkspaceService
    {
        Task<AccountWorkspaceDetailsDto?> GetDetailsAsync(int ownerUserId);

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

        Task<(Stream Stream, string ContentType)?> OpenBrandLogoAsync(
            int ownerUserId
        );

        Task<(Stream Stream, string ContentType)?> OpenPublicBrandLogoAsync(
            string objectKey
        );
    }
}

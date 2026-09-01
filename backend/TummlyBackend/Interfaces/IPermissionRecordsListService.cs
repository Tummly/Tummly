using TummlyBackend.DTOs.PrivacyConsent;

namespace TummlyBackend.Interfaces
{
    public interface IPermissionRecordsListService
    {
        Task<object> ListAsync(
            PermissionRecordsListQuery query,
            CancellationToken cancellationToken = default
        );
    }
}

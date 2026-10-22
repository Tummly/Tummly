using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminCommercialCatalogService
    {
        Task<AdminCommercialCatalogVersionDto?> GetPublishedAsync(
            CancellationToken cancellationToken = default
        );

        Task<AdminCommercialCatalogVersionDto?> GetDraftAsync(
            CancellationToken cancellationToken = default
        );

        Task<(AdminCommercialCatalogVersionDto? Draft, string? Error)> EnsureDraftAsync(
            int actorAdminUserId,
            CancellationToken cancellationToken = default
        );

        Task<(AdminCommercialCatalogVersionDto? Draft, string? Error)> SaveDraftAsync(
            int actorAdminUserId,
            AdminCommercialCatalogSaveDraftRequest request,
            CancellationToken cancellationToken = default
        );

        Task<(AdminCommercialCatalogVersionDto? Published, string? Error)> PublishAsync(
            int actorAdminUserId,
            AdminCommercialCatalogPublishRequest request,
            CancellationToken cancellationToken = default
        );

        Task<PublicCustomUnitRatesDto?> GetPublicCustomUnitRatesAsync(
            CancellationToken cancellationToken = default
        );
    }
}

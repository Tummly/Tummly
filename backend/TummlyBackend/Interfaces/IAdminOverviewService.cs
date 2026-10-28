using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminOverviewService
    {
        Task<AdminOverviewDto> GetAsync(CancellationToken cancellationToken = default);
    }
}

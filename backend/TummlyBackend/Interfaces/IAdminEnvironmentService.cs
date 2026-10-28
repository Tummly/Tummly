using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    public interface IAdminEnvironmentService
    {
        Task<AdminEnvironmentDto> GetAsync(CancellationToken cancellationToken = default);
    }
}

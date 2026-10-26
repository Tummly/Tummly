using TummlyBackend.DTOs.AccountWorkspace;

namespace TummlyBackend.Interfaces
{
    public interface IGuestDataExportService
    {
        Task<(GuestDataExportResult? Result, string? Error, int StatusCode)>
            ExportAsync(int restaurantId, int actorUserId, string? format);
    }
}

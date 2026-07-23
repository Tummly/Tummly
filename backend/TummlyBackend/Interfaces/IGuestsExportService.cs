using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestsExportService
    {
        Task<GuestsExportResult> ExportAsync(GuestsExportQuery query);
    }
}

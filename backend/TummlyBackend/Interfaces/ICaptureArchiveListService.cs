using TummlyBackend.DTOs.Capture;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Capture Archive list module — SQL-composed search/filter/sort/page
    /// for archived QR codes (ADR-0024).
    /// </summary>
    public interface ICaptureArchiveListService
    {
        Task<object> ListAsync(CaptureArchiveListQuery query);
    }
}

using TummlyBackend.DTOs.Capture;

namespace TummlyBackend.Interfaces
{
    public interface ICapturePreviewOptionsService
    {
        Task<object> GetPreviewOptionsAsync(CapturePreviewOptionsQuery query);
    }
}

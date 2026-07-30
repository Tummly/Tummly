using TummlyBackend.DTOs.Capture;

namespace TummlyBackend.Interfaces
{
    public interface ICaptureLocationSnapshotService
    {
        Task<object> GetSnapshotAsync(CaptureLocationSnapshotQuery query);
    }
}

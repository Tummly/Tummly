using TummlyBackend.DTOs.Capture;

namespace TummlyBackend.Interfaces
{
    public interface ICaptureMultiLocationReadsService
    {
        Task<object> GetOverviewAsync(CaptureOverviewQuery query);

        Task<object> GetLocationsAsync(CaptureLocationsQuery query);
    }
}

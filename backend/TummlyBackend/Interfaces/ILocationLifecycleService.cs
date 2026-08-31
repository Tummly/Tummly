using TummlyBackend.DTOs.Locations;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Settings location lifecycle writes — Pause / Resume / Archive / Restore
    /// (ADR location settings; Capture cascade via
    /// <see cref="ICaptureQrLifecycleService"/>).
    /// </summary>
    public interface ILocationLifecycleService
    {
        Task<LocationLifecycleResult> PauseAsync(
            LocationLifecycleCommand command
        );

        Task<LocationLifecycleResult> ResumeAsync(
            LocationLifecycleCommand command
        );

        Task<LocationLifecycleResult> ArchiveAsync(
            LocationLifecycleCommand command
        );

        Task<LocationLifecycleResult> RestoreAsync(
            LocationLifecycleCommand command
        );
    }
}

using TummlyBackend.DTOs.Capture;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// QR lifecycle module — Capture QR code writes and Pause/Activate
    /// location capture for an Owned location (ADR-0025). Authz stays in
    /// HTTP; this seam returns typed domain results.
    /// </summary>
    public interface ICaptureQrLifecycleService
    {
        Task<QrLifecycleResult> CreateDigitalGuestLinkAsync(
            CreateDigitalGuestLinkCommand command
        );

        Task<QrLifecycleResult> UpdateInternalDescriptionAsync(
            UpdateInternalDescriptionCommand command
        );

        Task<QrLifecycleResult> PauseAsync(QrCodeLifecycleCommand command);

        Task<QrLifecycleResult> ResumeAsync(QrCodeLifecycleCommand command);

        Task<QrLifecycleResult> RotateAsync(QrCodeLifecycleCommand command);

        Task<QrLifecycleResult> ArchiveAsync(QrCodeLifecycleCommand command);

        Task<QrLifecycleResult> RestoreAsync(QrCodeLifecycleCommand command);

        Task<QrLifecycleResult> PauseLocationCaptureAsync(
            LocationCaptureLifecycleCommand command
        );

        Task<QrLifecycleResult> ActivateLocationCaptureAsync(
            LocationCaptureLifecycleCommand command
        );
    }
}

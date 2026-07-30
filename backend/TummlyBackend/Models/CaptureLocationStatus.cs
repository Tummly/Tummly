namespace TummlyBackend.Models
{
    /// <summary>
    /// Whether Capture is enabled for an Owned location as a whole.
    /// Distinct from an individual QR code's Active / Paused / Archived status.
    /// See CONTEXT.md "Capture location status".
    /// </summary>
    public enum CaptureLocationStatus
    {
        Active = 0,

        Paused = 1,
    }
}

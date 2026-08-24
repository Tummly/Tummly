namespace TummlyBackend.Models
{
    /// <summary>
    /// Whether the Restaurant workspace is operating or paused account-wide.
    /// Distinct from per-location Capture location status.
    /// </summary>
    public enum WorkspaceStatus
    {
        Active = 0,

        Paused = 1,
    }
}

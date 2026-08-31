namespace TummlyBackend.Models
{
    /// <summary>
    /// Settings lifecycle for an Owned location (Draft / Active / Paused /
    /// Archived). Distinct from <see cref="CaptureLocationStatus"/>.
    /// See CONTEXT.md "Location lifecycle status".
    /// </summary>
    public enum LocationLifecycleStatus
    {
        Draft = 0,

        Active = 1,

        Paused = 2,

        Archived = 3,
    }
}

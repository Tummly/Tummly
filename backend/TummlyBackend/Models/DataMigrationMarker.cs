using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Durable one-row markers for finite startup backfills.
    /// Guest-tag union cannot be EXISTS-gated cheaply; after a successful full
    /// pass we insert Id = <see cref="DataMigrationMarkerIds.GuestTagBackfill"/>
    /// so later boots skip the keyset scan. Clear the row to re-run.
    /// </summary>
    public class DataMigrationMarker
    {
        [MaxLength(64)]
        public string Id { get; set; } = string.Empty;

        public DateTime CompletedAt { get; set; }
    }

    public static class DataMigrationMarkerIds
    {
        public const string GuestTagBackfill = "guest-tag-backfill";
    }
}

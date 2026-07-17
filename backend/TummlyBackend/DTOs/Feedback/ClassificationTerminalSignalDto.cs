namespace TummlyBackend.DTOs.Feedback
{
    /// <summary>
    /// Thin Feedback/Home SignalR payload — REST remains source of truth for classification fields.
    /// </summary>
    public sealed class ClassificationTerminalSignalDto
    {
        public int FeedbackId { get; set; }

        public int LocationId { get; set; }
    }
}

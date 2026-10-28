using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Per-visitor public-demo mutation overlay (§15). Baseline seed rows stay
    /// unchanged; demo JWT reads merge these patches for the session id.
    /// </summary>
    public class DemoSessionOverlay
    {
        public long Id { get; set; }

        public Guid SessionId { get; set; }

        /// <summary>feedback | offer_issue | campaign_draft | recovery_draft</summary>
        [Required]
        [MaxLength(64)]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string EntityKey { get; set; } = string.Empty;

        [Required]
        public string PayloadJson { get; set; } = "{}";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }
    }

    public static class DemoSessionOverlayEntityTypes
    {
        public const string FeedbackWorkflow = "feedback_workflow";
        public const string OfferRedemption = "offer_redemption";
        public const string RecoveryDraft = "recovery_draft";
        public const string CampaignDraft = "campaign_draft";
        public const string SimulatedSend = "simulated_send";

        /// <summary>Assistant thread id created by a demo session; only that session may read or continue it.</summary>
        public const string AssistantConversation = "assistant_conversation";
    }
}

namespace TummlyBackend.Helpers
{
    public static class BilledAiPackKeys
    {
        public const string RecoveryAiDraftCompleted = "recovery_ai_draft_completed";

        public const string CampaignAiDraftCompleted = "campaign_ai_draft_completed";

        public const string OperatorRegenerationCompleted =
            "operator_regeneration_completed";

        public static string ForRecovery(string mode)
            => Resolve(isRecovery: true, mode);

        public static string ForCampaign(string mode)
            => Resolve(isRecovery: false, mode);

        private static string Resolve(bool isRecovery, string mode)
        {
            var normalized = mode.Trim().ToLowerInvariant();
            return normalized switch
            {
                "prepare" => isRecovery
                    ? RecoveryAiDraftCompleted
                    : CampaignAiDraftCompleted,
                "rewrite_subject" or "rewrite_message" =>
                    OperatorRegenerationCompleted,
                _ => throw new ArgumentException(
                    "Mode must be prepare, rewrite_subject, or rewrite_message."
                ),
            };
        }
    }
}

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Closed Assistant task set emitted by the live-answer model or Fake.
    /// The server binds tools and may downgrade Create → Refuse. It must not
    /// upgrade Retrieve or Refuse → Create.
    /// </summary>
    public static class AssistantTask
    {
        public const string Retrieve = "retrieve";
        public const string CreateCampaignDraft = "create-campaign-draft";
        public const string OfferPath = "offer-path";
        public const string RecoveryPath = "recovery-path";
        public const string Refuse = "refuse";

        public static readonly string[] All =
        [
            Retrieve,
            CreateCampaignDraft,
            OfferPath,
            RecoveryPath,
            Refuse,
        ];

        public static bool IsKnown(string? value)
            => value is not null
                && All.Contains(value, StringComparer.Ordinal);

        public static string Normalize(string? value)
            => IsKnown(value) ? value! : Retrieve;
    }
}

namespace TummlyBackend.Notifications
{
    /// <summary>
    /// Locked Tips + Product update seeds ensured on Operator shell connect.
    /// Copy from Operator Notifications decision package.
    /// </summary>
    public static class NotificationSeeds
    {
        public sealed record SeedDefinition(
            string Type,
            string Title,
            string Body,
            string? CtaLabel,
            string? CtaHref
        );

        public static readonly IReadOnlyList<SeedDefinition> All =
        [
            new(
                "tip-place-qr-materials",
                "Place your QR where guests already pause",
                "Put starter QR materials at the counter, collection point, or in delivery bags so guests can scan while they wait — that’s when feedback and sign-ups stick.",
                "View setup guide",
                "/help-center/articles/getting-started"
            ),
            new(
                "tip-preview-guest-form",
                "Preview your guest form before guests do",
                "Open Preview guest form on Home to walk the Smart Guest Link yourself — confirm the experience, then place your QR with confidence.",
                null,
                null
            ),
            new(
                "product-operator-home-live",
                "Your Operator Home is live",
                "Track guest feedback in Latest activity, finish Guest Loop setup on Home, and download your QR when you’re ready to place materials.",
                null,
                null
            ),
        ];
    }
}

namespace TummlyBackend.Billing
{
    public static class CancelPlanReasons
    {
        public const string TooExpensive = "too_expensive";

        public const string NotEnoughScans = "not_enough_scans";

        public const string NotUsingCampaigns = "not_using_campaigns";

        public const string MissingFeature = "missing_feature";

        public const string SwitchedProvider = "switched_provider";

        public const string BusinessClosed = "business_closed";

        public const string Other = "other";

        public static readonly HashSet<string> Allowed =
        [
            TooExpensive,
            NotEnoughScans,
            NotUsingCampaigns,
            MissingFeature,
            SwitchedProvider,
            BusinessClosed,
            Other,
        ];
    }
}

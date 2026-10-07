namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Wire <c>reason</c> values on soft not-ready Weekly brief generate envelopes.
    /// Reports maps known reasons to empty-state helper copy.
    /// </summary>
    public static class WeeklyBriefNotReadyReasons
    {
        public const string LocationTooNew = "location-too-new";

        public const string Pilot = "pilot";

        public const string NoClosedOverlap = "no-closed-overlap";

        public const string LocationMissing = "location-missing";
    }
}

using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Derives the legacy <see cref="LocationGuestMarketingPreference"/> rollup
    /// from email and SMS permission states. Feedback follow-up does not
    /// affect this rollup.
    /// </summary>
    /// <remarks>
    /// Rollup rule (Privacy &amp; consent ticket 01 / PRD):
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="LocationGuestMarketingPreference.Allowed"/> when email
    /// marketing OR SMS marketing is <see cref="LocationGuestPermissionState.Granted"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="LocationGuestMarketingPreference.NotRecorded"/> when both
    /// email and SMS are
    /// <see cref="LocationGuestPermissionState.NotRecorded"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="LocationGuestMarketingPreference.OptedOut"/> otherwise
    /// (including both withdrawn, or one withdrawn and the other not recorded).
    /// </description>
    /// </item>
    /// </list>
    /// Legacy migration maps Allowed → all three granted, Opted out → all
    /// three withdrawn, Not recorded → all three not recorded; this rollup
    /// is the inverse for email/SMS channels.
    /// </remarks>
    public static class LocationGuestMarketingPreferenceRollup
    {
        public static LocationGuestMarketingPreference Derive(
            LocationGuestPermissionState emailMarketing,
            LocationGuestPermissionState smsMarketing
        )
        {
            if (
                emailMarketing == LocationGuestPermissionState.Granted
                || smsMarketing == LocationGuestPermissionState.Granted
            )
            {
                return LocationGuestMarketingPreference.Allowed;
            }

            if (
                emailMarketing == LocationGuestPermissionState.NotRecorded
                && smsMarketing == LocationGuestPermissionState.NotRecorded
            )
            {
                return LocationGuestMarketingPreference.NotRecorded;
            }

            return LocationGuestMarketingPreference.OptedOut;
        }

        public static LocationGuestMarketingPreference Derive(
            IReadOnlyDictionary<
                LocationGuestPermissionKind,
                LocationGuestPermissionState
            > states
        ) =>
            Derive(
                states[LocationGuestPermissionKind.EmailMarketing],
                states[LocationGuestPermissionKind.SmsMarketing]
            );
    }
}

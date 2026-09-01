using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class LocationGuestPermissionPresentation
    {
        public static string PermissionLabel(LocationGuestPermissionKind kind) =>
            kind switch
            {
                LocationGuestPermissionKind.EmailMarketing => "Email marketing",
                LocationGuestPermissionKind.SmsMarketing => "SMS marketing",
                LocationGuestPermissionKind.FeedbackFollowUp =>
                    "Feedback follow-up",
                _ => kind.ToString(),
            };

        public static string SourceLabel(string source) =>
            source switch
            {
                LocationGuestPermissionLedgerSources.GuestForm => "Guest form",
                LocationGuestPermissionLedgerSources.Operator => "Operator",
                LocationGuestPermissionLedgerSources.LegacyMarketingPreference =>
                    "Legacy marketing preference",
                _ => source,
            };
    }
}

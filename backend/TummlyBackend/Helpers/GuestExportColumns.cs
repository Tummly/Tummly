using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared Guest export column contract — Guests list CSV and Account
    /// Workspace Guest-data CSV/XLSX must emit the same headers and value shapes.
    /// </summary>
    public static class GuestExportColumns
    {
        public static readonly string[] Headers =
        [
            "Name",
            "Email",
            "Mobile",
            "Location",
            "Marketing preference",
            "First captured",
        ];

        public static string FormatMarketingPreference(
            LocationGuestMarketingPreference preference
        ) =>
            preference switch
            {
                LocationGuestMarketingPreference.Allowed => "Allowed",
                LocationGuestMarketingPreference.OptedOut => "Opted out",
                LocationGuestMarketingPreference.NotRecorded => "Not recorded",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(preference),
                    preference,
                    "Unknown Location Guest marketing preference."
                ),
            };

        public static string FormatIsoUtc(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

            return utc.ToString("O");
        }

        public static IReadOnlyList<string> ToRow(
            string name,
            string? email,
            string? mobile,
            string locationName,
            LocationGuestMarketingPreference marketingPreference,
            DateTime firstCaptured
        ) =>
            [
                name,
                email ?? string.Empty,
                mobile ?? string.Empty,
                locationName,
                FormatMarketingPreference(marketingPreference),
                FormatIsoUtc(firstCaptured),
            ];
    }
}

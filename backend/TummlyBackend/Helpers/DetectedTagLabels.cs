using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Backend DetectedTag → display-label map aligned with FE
    /// <c>labelForDetectedTag</c> / <c>DETECTED_TAG_LABELS</c>.
    /// </summary>
    public static class DetectedTagLabels
    {
        private static readonly IReadOnlyDictionary<DetectedTag, string> Labels =
            new Dictionary<DetectedTag, string>
            {
                [DetectedTag.FoodQuality] = "Food quality",
                [DetectedTag.Service] = "Service",
                [DetectedTag.WaitTime] = "Wait time",
                [DetectedTag.Cleanliness] = "Cleanliness",
                [DetectedTag.Value] = "Value",
                [DetectedTag.Atmosphere] = "Atmosphere",
                [DetectedTag.Billing] = "Billing",
                [DetectedTag.AllergiesDietary] = "Allergies & dietary",
                [DetectedTag.BookingSeating] = "Booking & seating",
                [DetectedTag.Other] = "Other",
            };

        public static string For(DetectedTag tag)
            => Labels.TryGetValue(tag, out var label)
                ? label
                : tag.ToString();

        public static bool TryParseKey(string? key, out DetectedTag tag)
            => Enum.TryParse(key, ignoreCase: false, out tag)
                && Enum.IsDefined(tag);
    }
}

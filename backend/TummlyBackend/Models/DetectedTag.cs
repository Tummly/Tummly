namespace TummlyBackend.Models
{
    /// <summary>
    /// Closed topic-tag vocabulary for AI classification (phase 1 product-fixed).
    /// Independent of sentiment.
    /// </summary>
    public enum DetectedTag
    {
        FoodQuality = 0,

        Service = 1,

        WaitTime = 2,

        Cleanliness = 3,

        Value = 4,

        Atmosphere = 5,

        Billing = 6,

        AllergiesDietary = 7,

        BookingSeating = 8,

        /// <summary>Exclusive catch-all — never combined with other themes.</summary>
        Other = 9
    }
}

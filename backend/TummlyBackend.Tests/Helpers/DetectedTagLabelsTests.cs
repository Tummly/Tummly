using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class DetectedTagLabelsTests
    {
        [Theory]
        [InlineData(DetectedTag.FoodQuality, "Food quality")]
        [InlineData(DetectedTag.Service, "Service")]
        [InlineData(DetectedTag.WaitTime, "Wait time")]
        [InlineData(DetectedTag.Cleanliness, "Cleanliness")]
        [InlineData(DetectedTag.Value, "Value")]
        [InlineData(DetectedTag.Atmosphere, "Atmosphere")]
        [InlineData(DetectedTag.Billing, "Billing")]
        [InlineData(DetectedTag.AllergiesDietary, "Allergies & dietary")]
        [InlineData(DetectedTag.BookingSeating, "Booking & seating")]
        [InlineData(DetectedTag.Other, "Other")]
        public void For_MatchesFrontendLabelForDetectedTag(
            DetectedTag tag,
            string expected
        )
        {
            Assert.Equal(expected, DetectedTagLabels.For(tag));
        }

        [Fact]
        public void NormalizeName_CollapsesWhitespaceAndLowercases()
        {
            Assert.Equal(
                "vip guest",
                GuestTagNaming.NormalizeName("  VIP   Guest ")
            );
            Assert.Equal(
                "VIP Guest",
                GuestTagNaming.FormatDisplayName("  VIP   Guest ")
            );
        }
    }
}

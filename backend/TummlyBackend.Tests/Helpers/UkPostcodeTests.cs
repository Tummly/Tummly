using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class UkPostcodeTests
    {
        [Theory]
        [InlineData("SW1A 1AA", true)]
        [InlineData("M14AB", true)]
        [InlineData("not-a-postcode", false)]
        public void IsValidFormat_matches_uk_postcode_pattern(
            string postcode,
            bool expected
        )
        {
            Assert.Equal(expected, UkPostcode.IsValidFormat(postcode));
        }

        [Theory]
        [InlineData("sw1a 1aa", "SW1A1AA")]
        [InlineData(" M1 4AB ", "M14AB")]
        public void NormalizeForLookup_removes_spaces_and_uppercases(
            string input,
            string expected
        )
        {
            Assert.Equal(expected, UkPostcode.NormalizeForLookup(input));
        }

        [Fact]
        public void FormatForDisplay_adds_space_before_inward_code()
        {
            Assert.Equal("SW1A 1AA", UkPostcode.FormatForDisplay("SW1A1AA"));
        }
    }
}

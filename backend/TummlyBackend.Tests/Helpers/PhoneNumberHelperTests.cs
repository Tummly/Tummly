using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class PhoneNumberHelperTests
    {
        private const string UkMobileE164 = "+447911123456";

        [Theory]
        [InlineData("07911123456", UkMobileE164)]
        [InlineData("+447911123456", UkMobileE164)]
        [InlineData("+44 7911 123 456", UkMobileE164)]
        [InlineData("447911123456", UkMobileE164)]
        [InlineData("0044 7911 123456", UkMobileE164)]
        [InlineData("(07911) 123-456", UkMobileE164)]
        [InlineData("02079460000", "+442079460000")]
        public void NormalizeToE164_AcceptsCommonUkFormats(
            string input,
            string expected
        )
        {
            Assert.Equal(
                expected,
                PhoneNumberHelper.NormalizeToE164(input)
            );
        }

        [Theory]
        [InlineData("+923156878896")]
        [InlineData("+14155552671")]
        public void NormalizeToE164_RejectsInternationalNumbers(string input)
        {
            Assert.False(
                PhoneNumberHelper.TryNormalizeToE164(
                    input,
                    PhoneNumberHelper.DefaultRegion,
                    out _
                )
            );
        }

        [Theory]
        [InlineData("03156878896")]
        [InlineData("07700900123")]
        [InlineData("12345")]
        [InlineData("not-a-phone")]
        public void TryNormalizeToE164_RejectsInvalidNumbers(string input)
        {
            Assert.False(
                PhoneNumberHelper.TryNormalizeToE164(
                    input,
                    PhoneNumberHelper.DefaultRegion,
                    out _
                )
            );
        }

        [Theory]
        [InlineData("07911123456", UkMobileE164)]
        [InlineData("+44 7911 123 456", UkMobileE164)]
        public void TryNormalizeUkMobileToE164_AcceptsUkMobiles(
            string input,
            string expected
        )
        {
            Assert.True(
                PhoneNumberHelper.TryNormalizeUkMobileToE164(
                    input,
                    PhoneNumberHelper.DefaultRegion,
                    out var e164
                )
            );
            Assert.Equal(expected, e164);
        }

        [Theory]
        [InlineData("07700 900123", "+447700900123")]
        [InlineData("+44 7700 900123", "+447700900123")]
        [InlineData("07700900999", "+447700900999")]
        public void TryNormalizeUkMobileToE164_ReservedRange_AcceptedWhenPolicyAllows(
            string input,
            string expected
        )
        {
            using var policy = ReservedTestNumberPolicy.OverrideFor(true);

            Assert.True(
                PhoneNumberHelper.TryNormalizeUkMobileToE164(
                    input,
                    PhoneNumberHelper.DefaultRegion,
                    out var e164
                )
            );
            Assert.Equal(expected, e164);
        }

        [Theory]
        [InlineData("07700 900123")]
        [InlineData("+44 7700 900123")]
        public void TryNormalizeUkMobileToE164_ReservedRange_RejectedWhenPolicyOff(string input)
        {
            using var policy = ReservedTestNumberPolicy.OverrideFor(false);

            Assert.False(
                PhoneNumberHelper.TryNormalizeUkMobileToE164(
                    input,
                    PhoneNumberHelper.DefaultRegion,
                    out _
                )
            );
        }

        [Fact]
        public void TryNormalizeToE164_ReservedRange_RejectedEvenWhenPolicyAllows()
        {
            // Shared path (operator phone, live verification) never takes the drama range.
            using var policy = ReservedTestNumberPolicy.OverrideFor(true);

            Assert.False(
                PhoneNumberHelper.TryNormalizeToE164(
                    "07700 900123",
                    PhoneNumberHelper.DefaultRegion,
                    out _
                )
            );
        }

        [Theory]
        [InlineData("07700900000", true)]
        [InlineData("07700 900999", true)]
        [InlineData("+447700900123", true)]
        [InlineData("+44 7700 900 123", true)]
        [InlineData("00447700900123", true)]
        [InlineData("07700901000", false)]
        [InlineData("+447700901000", false)]
        [InlineData("07911123456", false)]
        [InlineData("+923156878896", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsReservedTestRange_MatchesOnlyTheDramaRangeInAnySpelling(string? phone, bool expected)
        {
            Assert.Equal(expected, PhoneNumberHelper.IsReservedTestRange(phone));
        }

        [Theory]
        [InlineData("02079460000")]
        [InlineData("+44 20 7946 0000")]
        [InlineData("+923156878896")]
        [InlineData("not-a-phone")]
        public void TryNormalizeUkMobileToE164_RejectsLandlinesAndNonUk(
            string input
        )
        {
            Assert.False(
                PhoneNumberHelper.TryNormalizeUkMobileToE164(
                    input,
                    PhoneNumberHelper.DefaultRegion,
                    out _
                )
            );
        }

        [Fact]
        public void NormalizeOptional_ReturnsNullForBlankValues()
        {
            Assert.Null(PhoneNumberHelper.NormalizeOptional(null));
            Assert.Null(PhoneNumberHelper.NormalizeOptional("   "));
        }

        [Fact]
        public void FormatForDisplay_FormatsUkNumbersNationally()
        {
            Assert.Equal(
                "07911 123456",
                PhoneNumberHelper.FormatForDisplay(UkMobileE164)
            );
        }
    }
}

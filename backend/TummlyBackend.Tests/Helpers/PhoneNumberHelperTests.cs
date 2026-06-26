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

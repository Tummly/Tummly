using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class LogContactMaskTests
    {
        [Theory]
        [InlineData("alice@example.com", "a•••@example.com")]
        [InlineData("A@b.co", "A•••@b.co")]
        [InlineData("  bob@example.org  ", "b•••@example.org")]
        public void Email_MasksLocalPartKeepingFirstCharAndDomain(
            string input,
            string expected
        )
        {
            Assert.Equal(expected, LogContactMask.Email(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("@missing-local.com")]
        [InlineData("missing-domain@")]
        public void Email_BlankOrInvalid_ReturnsFourBullets(string? input)
        {
            Assert.Equal("••••", LogContactMask.Email(input));
        }

        [Theory]
        [InlineData("+447911123456", "••••3456")]
        [InlineData("07911123456", "••••3456")]
        [InlineData("+15005550006", "••••0006")]
        [InlineData("1234", "••••1234")]
        public void Phone_KeepsLastFourDigits(string input, string expected)
        {
            Assert.Equal(expected, LogContactMask.Phone(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12")]
        [InlineData("abc")]
        public void Phone_BlankOrFewerThanFourDigits_ReturnsFourBullets(
            string? input
        )
        {
            Assert.Equal("••••", LogContactMask.Phone(input));
        }

        [Fact]
        public void Scrub_ReplacesEmailAndPhoneLikeSubstrings()
        {
            var raw =
                "Failed for alice@example.com to +447911123456 (code 21211)";

            var scrubbed = LogContactMask.Scrub(raw);

            Assert.DoesNotContain("alice@example.com", scrubbed);
            Assert.DoesNotContain("+447911123456", scrubbed);
            Assert.DoesNotContain("447911123456", scrubbed);
            Assert.Contains("a•••@example.com", scrubbed);
            Assert.Contains("••••3456", scrubbed);
            Assert.Contains("21211", scrubbed);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void Scrub_NullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            Assert.Equal(expected, LogContactMask.Scrub(input));
        }
    }
}

using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class ShopInterestEmailMaskTests
    {
        [Theory]
        [InlineData("jane.doe@example.com", "ja***@example.com")]
        [InlineData("ab@example.com", "a***@example.com")]
        [InlineData("a@example.com", "***@example.com")]
        public void Mask_KeepsAtMostTwoLocalCharacters_AndTheDomain(
            string email,
            string expected
        )
        {
            Assert.Equal(expected, ShopInterestEmailMask.Mask(email));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("no-at-sign")]
        [InlineData("@example.com")]
        [InlineData("jane@")]
        public void Mask_ReturnsPlaceholder_ForMalformedInput(string email)
        {
            Assert.Equal("***", ShopInterestEmailMask.Mask(email));
        }

        [Fact]
        public void Mask_NeverReturnsTheFullLocalPart()
        {
            var masked = ShopInterestEmailMask.Mask("owner@venue.co.uk");

            Assert.DoesNotContain("owner@", masked);
            Assert.EndsWith("@venue.co.uk", masked);
        }
    }
}

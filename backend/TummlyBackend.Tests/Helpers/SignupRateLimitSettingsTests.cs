using TummlyBackend.Configurations;

namespace TummlyBackend.Tests.Helpers
{
    public class SignupRateLimitSettingsTests
    {
        [Fact]
        public void Validate_Defaults_Pass()
        {
            new SignupRateLimitSettings().Validate();
        }

        [Fact]
        public void Validate_Defaults_MatchRuling12()
        {
            var settings = new SignupRateLimitSettings();

            Assert.Equal(10, settings.PerEndpointPerMinute);
            Assert.Equal(30, settings.CombinedPerMinute);
            Assert.Equal(120, settings.TokenEndpointPerMinute);
            Assert.Equal(1, settings.TrustedProxyHops);
        }

        [Theory]
        [InlineData("PerEndpointPerMinute")]
        [InlineData("CombinedPerMinute")]
        [InlineData("TokenEndpointPerMinute")]
        [InlineData("TrustedProxyHops")]
        public void Validate_ZeroOrNegative_Throws(string property)
        {
            foreach (var bad in new[] { 0, -1 })
            {
                var settings = new SignupRateLimitSettings();
                typeof(SignupRateLimitSettings).GetProperty(property)!.SetValue(settings, bad);

                var ex = Assert.Throws<InvalidOperationException>(settings.Validate);

                Assert.Contains(property, ex.Message, StringComparison.Ordinal);
            }
        }
    }
}

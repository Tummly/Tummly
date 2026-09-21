using TummlyBackend.Configurations;

namespace TummlyBackend.Tests.Services
{
    public class TummlySellerVatSettingsTests
    {
        [Fact]
        public void EffectiveVatRateBps_IsZero_WhenModeOffOrUnset()
        {
            var off = new TummlySellerVatSettings { IsActive = false };
            Assert.False(off.IsActive);
            Assert.Equal(0, off.EffectiveVatRateBps);

            var unset = new TummlySellerVatSettings();
            Assert.False(unset.IsActive);
            Assert.Equal(0, unset.EffectiveVatRateBps);
        }

        [Fact]
        public void EffectiveVatRateBps_Is2000_WhenModeActive()
        {
            var active = new TummlySellerVatSettings { IsActive = true };
            Assert.True(active.IsActive);
            Assert.Equal(2000, active.EffectiveVatRateBps);
        }
    }
}

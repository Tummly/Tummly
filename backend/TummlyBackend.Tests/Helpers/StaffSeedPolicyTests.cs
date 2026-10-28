using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class StaffSeedPolicyTests
    {
        private const string DevEmail = "admin@tummly.com";
        private const string DevPassword = "Admin@123";
        private const string ConfiguredEmail = "owner@tummly.example";
        private const string ConfiguredPassword = "Long-Random-Pass-1";

        [Fact]
        public void Development_NoConfig_FallsBackToDevelopmentDefaults()
        {
            var decision = StaffSeedPolicy.Decide(
                isDevelopment: true,
                configuredEmail: null,
                configuredPassword: null,
                developmentEmail: DevEmail,
                developmentPassword: DevPassword
            );

            Assert.True(decision.ShouldSeed);
            Assert.Equal(DevEmail, decision.Email);
            Assert.Equal(DevPassword, decision.Password);
            Assert.Equal(
                StaffSeedPasswordSource.DevelopmentDefault,
                decision.PasswordSource
            );
        }

        [Fact]
        public void Development_ConfiguredPassword_UsesConfiguration()
        {
            var decision = StaffSeedPolicy.Decide(
                isDevelopment: true,
                configuredEmail: ConfiguredEmail,
                configuredPassword: ConfiguredPassword,
                developmentEmail: DevEmail,
                developmentPassword: DevPassword
            );

            Assert.True(decision.ShouldSeed);
            Assert.Equal(ConfiguredEmail, decision.Email);
            Assert.Equal(ConfiguredPassword, decision.Password);
            Assert.Equal(StaffSeedPasswordSource.Configuration, decision.PasswordSource);
        }

        [Fact]
        public void Production_NoConfig_DoesNotSeed()
        {
            var decision = StaffSeedPolicy.Decide(
                isDevelopment: false,
                configuredEmail: null,
                configuredPassword: null,
                developmentEmail: DevEmail,
                developmentPassword: DevPassword
            );

            Assert.False(decision.ShouldSeed);
            Assert.Equal(StaffSeedPasswordSource.None, decision.PasswordSource);
            Assert.Equal(string.Empty, decision.Password);
        }

        [Fact]
        public void Production_ConfiguredPassword_SeedsWithConfiguredValues()
        {
            var decision = StaffSeedPolicy.Decide(
                isDevelopment: false,
                configuredEmail: ConfiguredEmail,
                configuredPassword: ConfiguredPassword,
                developmentEmail: DevEmail,
                developmentPassword: DevPassword
            );

            Assert.True(decision.ShouldSeed);
            Assert.Equal(ConfiguredEmail, decision.Email);
            Assert.Equal(ConfiguredPassword, decision.Password);
            Assert.Equal(StaffSeedPasswordSource.Configuration, decision.PasswordSource);
        }

        [Fact]
        public void Qa_NoConfig_DoesNotUseHardCodedPassword()
        {
            // QA is a non-Development environment: the fallback must not apply.
            var decision = StaffSeedPolicy.Decide(
                isDevelopment: false,
                configuredEmail: "admin@tummly.com",
                configuredPassword: "   ",
                developmentEmail: DevEmail,
                developmentPassword: DevPassword
            );

            Assert.False(decision.ShouldSeed);
            Assert.Equal(StaffSeedPasswordSource.None, decision.PasswordSource);
            Assert.NotEqual(DevPassword, decision.Password);
        }

        [Fact]
        public void NoConfiguredEmail_UsesDefaultEmailAndConfiguredPassword()
        {
            var decision = StaffSeedPolicy.Decide(
                isDevelopment: false,
                configuredEmail: "  ",
                configuredPassword: ConfiguredPassword,
                developmentEmail: DevEmail,
                developmentPassword: DevPassword
            );

            Assert.True(decision.ShouldSeed);
            Assert.Equal(DevEmail, decision.Email);
            Assert.Equal(ConfiguredPassword, decision.Password);
        }
    
        [Fact]
        public void UsesDevelopmentDefaultPassword_DetectsOnlyTheOldDefault()
        {
            var oldHash = BCrypt.Net.BCrypt.HashPassword(StaffSeedPolicy.DevelopmentAdminPassword);
            var rotatedHash = BCrypt.Net.BCrypt.HashPassword("Rotated-Long-Pass-9");

            Assert.True(StaffSeedPolicy.UsesDevelopmentDefaultPassword("admin@tummly.com", oldHash));
            Assert.False(StaffSeedPolicy.UsesDevelopmentDefaultPassword("admin@tummly.com", rotatedHash));
            Assert.False(StaffSeedPolicy.UsesDevelopmentDefaultPassword("other@tummly.com", oldHash));
            Assert.False(StaffSeedPolicy.UsesDevelopmentDefaultPassword("admin@tummly.com", "not-a-hash"));
        }
    }
}

using TummlyBackend.Helpers;
using Xunit;

namespace TummlyBackend.Tests.Services
{
    public sealed class StaffPasswordPolicyTests
    {
        private const string Email = "ops@tummly.com";

        [Fact]
        public void Validate_ElevenCharacters_IsTooShort()
        {
            Assert.Equal(
                StaffPasswordPolicy.TooShort,
                StaffPasswordPolicy.Validate("Abcdefghij1", Email)
            );
        }

        [Fact]
        public void Validate_TwelveCharacters_IsAccepted()
        {
            Assert.Null(StaffPasswordPolicy.Validate("Abcdefghij12", Email));
        }

        [Fact]
        public void Validate_SeventyTwoBytes_IsAccepted()
        {
            var password = new string('a', 71) + "B";

            Assert.Null(StaffPasswordPolicy.Validate(password, Email));
        }

        [Fact]
        public void Validate_SeventyThreeBytes_IsTooLong()
        {
            var password = new string('a', 72) + "B";

            Assert.Equal(
                StaffPasswordPolicy.TooLong,
                StaffPasswordPolicy.Validate(password, Email)
            );
        }

        [Fact]
        public void Validate_EqualToEmail_IgnoringCase_IsRefused()
        {
            Assert.Equal(
                StaffPasswordPolicy.MatchesEmail,
                StaffPasswordPolicy.Validate("OPS@tummly.com", Email)
            );
        }

        [Theory]
        [InlineData("Admin@123")]
        [InlineData("Support@123")]
        public void Validate_DevelopmentDefaults_AreRefusedBeforeLengthCheck(string password)
        {
            Assert.Equal(
                StaffPasswordPolicy.DevelopmentDefault,
                StaffPasswordPolicy.Validate(password, Email)
            );
        }

        [Fact]
        public void Validate_Null_IsTooShort()
        {
            Assert.Equal(StaffPasswordPolicy.TooShort, StaffPasswordPolicy.Validate(null, Email));
        }
    
        [Fact]
        public void Validate_RefusesPasswordsOverBcrypt72Bytes()
        {
            Assert.Null(StaffPasswordPolicy.Validate(new string('a', 72), "x@example.com"));
            Assert.Equal(
                StaffPasswordPolicy.TooLong,
                StaffPasswordPolicy.Validate(new string('a', 73), "x@example.com")
            );
            // 30 two-byte characters = 60 bytes, allowed; 40 = 80 bytes, refused.
            Assert.Null(StaffPasswordPolicy.Validate(new string('é', 30), "x@example.com"));
            Assert.Equal(
                StaffPasswordPolicy.TooLong,
                StaffPasswordPolicy.Validate(new string('é', 40), "x@example.com")
            );
        }
    }
}

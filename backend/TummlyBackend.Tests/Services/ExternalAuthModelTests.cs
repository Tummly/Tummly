using System.Reflection;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public class ExternalAuthModelTests
    {
        [Fact]
        public void User_PasswordHash_IsNullable()
        {
            var prop = typeof(User).GetProperty(nameof(User.PasswordHash));
            Assert.True(Nullable.GetUnderlyingType(prop!.PropertyType) != null
                || prop.PropertyType == typeof(string));
            // After change: string? — assert PropertyType allows null via NullabilityInfoContext
            var ctx = new NullabilityInfoContext();
            Assert.Equal(NullabilityState.Nullable, ctx.Create(prop).WriteState);
        }

        [Fact]
        public void PendingSignup_HasAuthProviderFields()
        {
            Assert.NotNull(typeof(PendingSignup).GetProperty("AuthProvider"));
            Assert.NotNull(typeof(PendingSignup).GetProperty("ProviderSubject"));
        }
    }
}

using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="GuestFormContactValidate"/> — Guest Form submit
    /// contact must be valid Email or UK mobile before upsert/ledger.
    /// </summary>
    public class GuestFormContactValidateTests
    {
        [Theory]
        [InlineData("guest@example.com", ContactType.Email)]
        [InlineData("07911123456", ContactType.Phone)]
        [InlineData("+447911123456", ContactType.Phone)]
        public void TryResolve_AcceptsValid(string contact, ContactType expected)
        {
            Assert.True(GuestFormContactValidate.TryResolve(contact, out var type));
            Assert.Equal(expected, type);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        [InlineData("12345")]
        [InlineData("+15551234567")] // US — not UK
        public void TryResolve_RejectsInvalid(string contact)
        {
            Assert.False(GuestFormContactValidate.TryResolve(contact, out var type));
            Assert.Equal(ContactType.Unknown, type);
        }
    }
}

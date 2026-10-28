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
        [InlineData("07123 456789", ContactType.Phone)]
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
        [InlineData("02079460000")] // UK landline — not mobile
        [InlineData("+44 20 7946 0000")]
        public void TryResolve_RejectsInvalid(string contact)
        {
            Assert.False(GuestFormContactValidate.TryResolve(contact, out var type));
            Assert.Equal(ContactType.Unknown, type);
        }

        [Theory]
        [InlineData("alex@example.com 07123456789")]
        [InlineData("07123456789 alex@example.com")]
        [InlineData("alex@example.com, bob@example.com")]
        [InlineData("alex@example.com;bob@example.com")]
        [InlineData("a@b@example.com")]
        [InlineData("07123456789 07987654321")]
        public void TryResolve_RejectsTwoContacts(string contact)
        {
            Assert.False(GuestFormContactValidate.TryResolve(contact, out var type));
            Assert.Equal(ContactType.Unknown, type);
        }
    }
}

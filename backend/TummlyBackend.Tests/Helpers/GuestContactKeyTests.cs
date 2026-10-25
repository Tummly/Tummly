using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Seam: <see cref="GuestContactKey"/> — normalised email:/phone: keys for
    /// WS2a contact hashing and suppression.
    /// </summary>
    public class GuestContactKeyTests
    {
        [Fact]
        public void TryCreate_Email_TrimsAndLowerCases()
        {
            Assert.True(
                GuestContactKey.TryCreate(
                    "  Alex@Example.COM ",
                    ContactType.Email,
                    out var key
                )
            );
            Assert.Equal("email:alex@example.com", key);
        }

        [Theory]
        [InlineData("07123 456789")]
        [InlineData("+447123456789")]
        public void TryCreate_Phone_NormalisesToSameE164(string input)
        {
            Assert.True(
                GuestContactKey.TryCreate(
                    input,
                    ContactType.Phone,
                    out var key
                )
            );
            Assert.Equal("phone:+447123456789", key);
        }

        [Fact]
        public void TryCreate_Unknown_ReturnsFalse()
        {
            Assert.False(
                GuestContactKey.TryCreate(
                    "anyone@example.com",
                    ContactType.Unknown,
                    out var key
                )
            );
            Assert.Equal(string.Empty, key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryCreate_Blank_ReturnsFalse(string? contact)
        {
            Assert.False(
                GuestContactKey.TryCreate(
                    contact,
                    ContactType.Email,
                    out _
                )
            );
            Assert.False(
                GuestContactKey.TryCreate(
                    contact,
                    ContactType.Phone,
                    out _
                )
            );
        }

        [Fact]
        public void TryCreate_InvalidPhone_ReturnsFalse()
        {
            Assert.False(
                GuestContactKey.TryCreate(
                    "not-a-phone",
                    ContactType.Phone,
                    out _
                )
            );
        }

        [Fact]
        public void FromMasterGuest_PrefersEmail()
        {
            var master = new MasterGuest
            {
                Email = "  Alex@Example.COM ",
                Mobile = "07123 456789",
            };

            Assert.Equal(
                "email:alex@example.com",
                GuestContactKey.FromMasterGuest(master)
            );
        }

        [Fact]
        public void FromMasterGuest_UsesMobileThenNormalizedPhone()
        {
            var fromMobile = new MasterGuest
            {
                Mobile = "07123 456789",
            };
            Assert.Equal(
                "phone:+447123456789",
                GuestContactKey.FromMasterGuest(fromMobile)
            );

            var fromNormalized = new MasterGuest
            {
                NormalizedPhone = "447123456789",
            };
            Assert.Equal(
                "phone:+447123456789",
                GuestContactKey.FromMasterGuest(fromNormalized)
            );
        }

        [Fact]
        public void FromMasterGuest_ReturnsNull_WhenNoneParse()
        {
            Assert.Null(
                GuestContactKey.FromMasterGuest(new MasterGuest())
            );
        }
    }
}

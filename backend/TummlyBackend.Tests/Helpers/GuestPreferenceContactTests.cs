using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers;

public class GuestPreferenceContactTests
{
    [Fact]
    public void TryParse_Email_TrimsAndLowers()
    {
        var ok = GuestPreferenceContact.TryParse(
            " Sophie@Example.com ",
            out var contactType,
            out var normalizedValue
        );

        Assert.True(ok);
        Assert.Equal(TummlyGuestContactTypes.Email, contactType);
        Assert.Equal("sophie@example.com", normalizedValue);
    }

    [Fact]
    public void TryParse_UkMobile_NationalFormat()
    {
        // 07700 900xxx is the Ofcom drama range and is rejected by
        // PhoneNumberHelper, so use a valid mobile number here.
        var ok = GuestPreferenceContact.TryParse(
            "07911 123456",
            out var contactType,
            out var normalizedValue
        );

        Assert.True(ok);
        Assert.Equal(TummlyGuestContactTypes.Phone, contactType);
        Assert.Equal("+447911123456", normalizedValue);
    }

    [Fact]
    public void TryParse_NonUkPhone_False()
    {
        var ok = GuestPreferenceContact.TryParse(
            "+12025550123",
            out var contactType,
            out var normalizedValue
        );

        Assert.False(ok);
        Assert.Equal(string.Empty, contactType);
        Assert.Equal(string.Empty, normalizedValue);
    }

    [Fact]
    public void TryParse_Garbage_False()
    {
        var ok = GuestPreferenceContact.TryParse(
            "abc",
            out var contactType,
            out var normalizedValue
        );

        Assert.False(ok);
        Assert.Equal(string.Empty, contactType);
        Assert.Equal(string.Empty, normalizedValue);
    }

    [Fact]
    public void TryParse_NullOrWhitespace_False()
    {
        Assert.False(GuestPreferenceContact.TryParse(null, out _, out _));
        Assert.False(GuestPreferenceContact.TryParse("   ", out _, out _));
    }

    [Fact]
    public void Mask_Email()
    {
        var result = GuestPreferenceContact.Mask(
            TummlyGuestContactTypes.Email,
            "sophie@example.com"
        );

        Assert.Equal("so•••••@example.com", result);
    }

    [Fact]
    public void Mask_Phone()
    {
        var result = GuestPreferenceContact.Mask(
            TummlyGuestContactTypes.Phone,
            "+447700900123"
        );

        Assert.Equal("•••• ••• 123", result);
    }
}

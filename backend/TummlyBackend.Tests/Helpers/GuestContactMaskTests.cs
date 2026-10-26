using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers;

public class GuestContactMaskTests
{
    [Fact]
    public void Mask_Email_KeepsFirstCharAndDomain()
    {
        var result = GuestContactMask.Mask(ContactType.Email, "alex@example.com");

        Assert.Equal("a••••@example.com", result);
    }

    [Fact]
    public void Mask_ShortLocal()
    {
        var result = GuestContactMask.Mask(ContactType.Email, "a@b.com");

        Assert.Equal("•@b.com", result);
    }

    [Fact]
    public void MaskEmail_Malformed_ReturnsDots()
    {
        Assert.Equal("••••", GuestContactMask.MaskEmail("bob"));
        Assert.Equal("••••", GuestContactMask.MaskEmail("@example.com"));
        Assert.Equal("••••", GuestContactMask.MaskEmail("bob@"));
    }

    [Fact]
    public void MaskPhone_LastFour()
    {
        var result = GuestContactMask.MaskPhone("07700 900987");

        Assert.Equal("••••0987", result);
    }

    [Fact]
    public void MaskPhone_TooShort()
    {
        var result = GuestContactMask.MaskPhone("12");

        Assert.Equal("••••", result);
    }

    [Fact]
    public void MaskEmail_NullOrBlank_ReturnsNull()
    {
        Assert.Null(GuestContactMask.MaskEmail(null));
        Assert.Null(GuestContactMask.MaskEmail(""));
        Assert.Null(GuestContactMask.MaskEmail("   "));
    }

    [Fact]
    public void MaskPhone_NullOrBlank_ReturnsNull()
    {
        Assert.Null(GuestContactMask.MaskPhone(null));
        Assert.Null(GuestContactMask.MaskPhone(""));
        Assert.Null(GuestContactMask.MaskPhone("   "));
    }

    [Fact]
    public void Mask_Unknown_ReturnsDots()
    {
        var result = GuestContactMask.Mask(ContactType.Unknown, "alex@example.com");

        Assert.Equal("••••", result);
    }

    [Fact]
    public void MaskOrEmpty_Blank_ReturnsEmpty()
    {
        Assert.Equal("", GuestContactMask.MaskOrEmpty(ContactType.Email, null));
        Assert.Equal("", GuestContactMask.MaskOrEmpty(ContactType.Phone, "   "));
    }

    [Fact]
    public void MaskOrEmpty_NonBlank_MatchesMask()
    {
        Assert.Equal(
            GuestContactMask.Mask(ContactType.Email, "alex@example.com"),
            GuestContactMask.MaskOrEmpty(ContactType.Email, "alex@example.com")
        );
    }
}

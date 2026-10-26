using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers;

public class GuestContactSearchTermTests
{
    [Fact]
    public void Parse_Email_CaseFolded()
    {
        var term = GuestContactSearchTerm.Parse("ALEX@Example.COM");

        Assert.Equal("alex@example.com", term.ExactEmail);
        Assert.Equal("alex@example.com", term.NameTerm);
        Assert.Empty(term.PhoneDigitForms);
    }

    [Fact]
    public void Parse_UkMobile_National_And_Intl_SameForms()
    {
        var national = GuestContactSearchTerm.Parse("07911 123456");
        var intl = GuestContactSearchTerm.Parse("+44 7911 123456");

        var expected = new[] { "447911123456", "07911123456" };
        Assert.Equal(expected, national.PhoneDigitForms);
        Assert.Equal(expected, intl.PhoneDigitForms);
        Assert.Null(national.ExactEmail);
        Assert.Null(intl.ExactEmail);
    }

    [Fact]
    public void Parse_PartialNumber_NoPhoneForms()
    {
        var term = GuestContactSearchTerm.Parse("0770090");

        Assert.Empty(term.PhoneDigitForms);
        Assert.Null(term.ExactEmail);
        Assert.Equal("0770090", term.NameTerm);
    }

    [Fact]
    public void Parse_Domain_NoEmail()
    {
        var term = GuestContactSearchTerm.Parse("example.com");

        Assert.Null(term.ExactEmail);
        Assert.Empty(term.PhoneDigitForms);
        Assert.Equal("example.com", term.NameTerm);
    }
}

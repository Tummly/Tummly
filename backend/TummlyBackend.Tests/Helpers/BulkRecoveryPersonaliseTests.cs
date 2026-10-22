using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers;

public class BulkRecoveryPersonaliseTests
{
    [Fact]
    public void EnsureGuestFirstNameToken_ReplacesLeadingHiName()
    {
        var body =
            "Hi James, Thank you for sharing your feedback about Main.";

        var result = BulkRecoveryPersonalise.EnsureGuestFirstNameToken(body);

        Assert.StartsWith(
            $"Hi {BulkRecoveryPersonalise.GuestFirstNameToken},",
            result,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("Hi James,", result, StringComparison.Ordinal);
    }

    [Fact]
    public void SubstituteGuestFirstName_UsesFirstToken()
    {
        var template =
            $"Hi {BulkRecoveryPersonalise.GuestFirstNameToken}, sorry.";

        var result = BulkRecoveryPersonalise.SubstituteGuestFirstName(
            template,
            "Maya Collins"
        );

        Assert.Equal("Hi Maya, sorry.", result);
    }

    [Fact]
    public void SubstituteGuestFirstName_FallsBackWhenNameMissing()
    {
        var template =
            $"Hi {BulkRecoveryPersonalise.GuestFirstNameToken},";

        var result = BulkRecoveryPersonalise.SubstituteGuestFirstName(
            template,
            null
        );

        Assert.Equal($"Hi {BulkRecoveryPersonalise.FallbackFirstName},", result);
    }
}

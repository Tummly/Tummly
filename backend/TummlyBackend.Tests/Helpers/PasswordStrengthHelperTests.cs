using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
  public class PasswordStrengthHelperTests
  {
    [Theory]
    [InlineData("", 0)]
    [InlineData("pass", 1)]
    [InlineData("password1", 2)]
    [InlineData("Password", 2)]
    [InlineData("Password1", 3)]
    [InlineData("Password1!", 4)]
    [InlineData("Password123!", 5)]
    public void GetScore_maps_passwords_to_expected_tiers(
      string password,
      int expectedScore
    )
    {
      Assert.Equal(
        (PasswordStrengthHelper.PasswordStrengthScore)expectedScore,
        PasswordStrengthHelper.GetScore(password)
      );
    }

    [Theory]
    [InlineData("Password1", true)]
    [InlineData("Password123!", true)]
    [InlineData("password1", false)]
    [InlineData("pass", false)]
    public void IsAtLeastGood_matches_good_threshold(
      string password,
      bool expected
    )
    {
      Assert.Equal(expected, PasswordStrengthHelper.IsAtLeastGood(password));
    }

    [Fact]
    public void GetValidationMessage_returns_min_length_for_short_passwords()
    {
      Assert.Equal(
        "Password must be at least 8 characters",
        PasswordStrengthHelper.GetValidationMessage("short")
      );
    }
  }
}

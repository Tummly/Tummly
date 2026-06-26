using System.Text.RegularExpressions;

namespace TummlyBackend.Helpers
{
  public static class PasswordStrengthHelper
  {
    public const int MinLength = 8;
    private const int StrongMinLength = 10;
    private const int ExcellentMinLength = 12;

    public enum PasswordStrengthScore
    {
      Empty = 0,
      VeryWeak = 1,
      Weak = 2,
      Good = 3,
      Strong = 4,
      Excellent = 5,
    }

    public static PasswordStrengthScore GetScore(string? password)
    {
      if (string.IsNullOrEmpty(password))
      {
        return PasswordStrengthScore.Empty;
      }

      if (password.Length < MinLength)
      {
        return PasswordStrengthScore.VeryWeak;
      }

      var hasUppercase = Regex.IsMatch(password, "[A-Z]");
      var hasNumber = Regex.IsMatch(password, "[0-9]");
      var hasSymbol = Regex.IsMatch(password, "[^A-Za-z0-9]");
      var hasNumberOrSymbol = hasNumber || hasSymbol;

      if (!hasUppercase || !hasNumberOrSymbol)
      {
        return PasswordStrengthScore.Weak;
      }

      if (
        password.Length >= ExcellentMinLength &&
        hasNumber &&
        hasSymbol
      )
      {
        return PasswordStrengthScore.Excellent;
      }

      if (password.Length >= StrongMinLength)
      {
        return PasswordStrengthScore.Strong;
      }

      return PasswordStrengthScore.Good;
    }

    public static bool IsAtLeastGood(string? password)
    {
      return GetScore(password) >= PasswordStrengthScore.Good;
    }

    public static string? GetValidationMessage(string? password)
    {
      if (string.IsNullOrEmpty(password))
      {
        return "Password is required.";
      }

      if (password.Length < MinLength)
      {
        return "Password must be at least 8 characters";
      }

      var hasNumber = Regex.IsMatch(password, "[0-9]");
      var hasSymbol = Regex.IsMatch(password, "[^A-Za-z0-9]");

      if (!hasNumber && !hasSymbol)
      {
        return "Password must include a number or symbol";
      }

      if (!Regex.IsMatch(password, "[A-Z]"))
      {
        return "Password must include an uppercase letter";
      }

      if (!IsAtLeastGood(password))
      {
        return "Password must include a number or symbol";
      }

      return null;
    }
  }
}

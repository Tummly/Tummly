using FluentValidation;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;

namespace TummlyBackend.Validators
{
  public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
  {
    public ResetPasswordDtoValidator()
    {
      RuleFor(x => x.Token).NotEmpty();

      RuleFor(x => x.NewPassword)
        .NotEmpty()
        .Must(PasswordStrengthHelper.IsAtLeastGood)
        .WithMessage(x =>
          PasswordStrengthHelper.GetValidationMessage(x.NewPassword)
          ?? "Password must reach Good strength or better.");

      RuleFor(x => x.ConfirmPassword)
        .Equal(x => x.NewPassword)
        .WithMessage("Passwords do not match");
    }
  }
}

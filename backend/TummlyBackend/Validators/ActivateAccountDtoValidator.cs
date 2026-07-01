using FluentValidation;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;

namespace TummlyBackend.Validators
{
    public class ActivateAccountDtoValidator
        : AbstractValidator<ActivateAccountDto>
    {
        public ActivateAccountDtoValidator()
        {
            RuleFor(x => x.ActivationCode)
                .NotEmpty()
                .WithMessage("Activation code is required.")
                .Must(code =>
                    ActivationCodeHelper.IsValidFormat(
                        ActivationCodeHelper.Normalize(code)
                    )
                )
                .WithMessage("Enter a valid activation code.");
        }
    }
}

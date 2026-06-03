using FluentValidation;
using TummlyBackend.DTOs.Trial;

namespace TummlyBackend.Validators
{
    public class TrialRequestValidator
        : AbstractValidator<TrialRequestDto>
    {
        public TrialRequestValidator()
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Business name is required.")
                .MinimumLength(2)
                .MaximumLength(100);

            RuleFor(x => x.BusinessCategory)
                .NotEmpty()
                .WithMessage("Business category is required.");

            RuleFor(x => x.Locations)
                .NotEmpty()
                .WithMessage("Locations field is required.");

            RuleFor(x => x.BusinessLink)
                .Must(link =>
                    string.IsNullOrWhiteSpace(link)
                    || Uri.IsWellFormedUriString(
                        link,
                        UriKind.Absolute
                    )
                )
                .WithMessage("Please enter a valid business URL.");

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MinimumLength(2)
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Please enter a valid email address.");

            RuleFor(x => x.Mobile)
                .NotEmpty()
                .WithMessage("Mobile number is required.")
                .Matches(@"^[0-9+\-\s]{10,15}$")
                .WithMessage("Please enter a valid mobile number.");

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Role is required.");

            RuleFor(x => x.Goal)
                .NotEmpty()
                .WithMessage("Goal is required.");

            RuleFor(x => x.TermsAccepted)
                .Equal(true)
                .WithMessage(
                    "You must accept terms and conditions."
                );
        }
    }
}
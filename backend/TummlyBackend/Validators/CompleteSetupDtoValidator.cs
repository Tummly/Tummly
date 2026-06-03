using FluentValidation;
using TummlyBackend.DTOs.Trial;

namespace TummlyBackend.Validators
{
    public class CompleteSetupDtoValidator : AbstractValidator<CompleteSetupDto>
    {
        public CompleteSetupDtoValidator()
        {
            RuleFor(x => x.Token).NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match");

            RuleFor(x => x.GroupName).NotEmpty();
            RuleFor(x => x.BusinessCategory).NotEmpty();
            RuleFor(x => x.PrimaryPhone).NotEmpty();

            RuleFor(x => x.Locations)
                .NotNull()
                .NotEmpty()
                .WithMessage("At least one location is required");

        }
    }
}
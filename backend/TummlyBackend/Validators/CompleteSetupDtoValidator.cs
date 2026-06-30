using FluentValidation;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Helpers;

namespace TummlyBackend.Validators
{
    public class CompleteSetupDtoValidator : AbstractValidator<CompleteSetupDto>
    {
        public CompleteSetupDtoValidator()
        {
            RuleFor(x => x.Token).NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty()
                .Must(PasswordStrengthHelper.IsAtLeastGood)
                .WithMessage(x =>
                    PasswordStrengthHelper.GetValidationMessage(x.Password)
                    ?? "Password must reach Good strength or better.");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match");

            RuleFor(x => x.GroupName).NotEmpty();
            RuleFor(x => x.BusinessCategory).NotEmpty();
            RuleFor(x => x.PrimaryPhone)
                .Must(phone =>
                    string.IsNullOrWhiteSpace(phone) ||
                    PhoneNumberHelper.TryNormalizeToE164(
                        phone,
                        PhoneNumberHelper.DefaultRegion,
                        out _
                    )
                )
                .WithMessage("Please enter a valid UK phone number.");

            RuleFor(x => x.Locations)
                .NotNull()
                .NotEmpty()
                .WithMessage("At least one location is required");

            RuleForEach(x => x.Locations).ChildRules(location =>
            {
                location.RuleFor(item => item.LocationName)
                    .NotEmpty()
                    .WithMessage("Location name is required.");

                location.RuleFor(item => item.Address)
                    .NotEmpty()
                    .WithMessage("Address is required.");

                location.RuleFor(item => item.Postcode)
                    .NotEmpty()
                    .WithMessage("Postcode is required.")
                    .Must(UkPostcode.IsValidFormat)
                    .WithMessage("Please enter a valid UK postcode.");

                location.RuleFor(item => item.LocationPhone)
                    .Must(phone =>
                        string.IsNullOrWhiteSpace(phone) ||
                        PhoneNumberHelper.TryNormalizeToE164(
                            phone,
                            PhoneNumberHelper.DefaultRegion,
                            out _
                        )
                    )
                    .WithMessage("Please enter a valid UK location phone number.");
            });
        }
    }
}

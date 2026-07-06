using FluentValidation;
using TummlyBackend.DTOs.HelpCentre;
using TummlyBackend.Models;

namespace TummlyBackend.Validators
{
    public class UpdateQueryStatusDtoValidator
        : AbstractValidator<UpdateQueryStatusDto>
    {
        public UpdateQueryStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(BeValidStatus)
                .WithMessage("Invalid query status.");

            RuleFor(x => x.EscalationNote)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.EscalationNote));
        }

        private static bool BeValidStatus(string status)
        {
            return HelpCentreQueryStatusExtensions.TryParseWireString(
                status,
                out _
            );
        }
    }
}

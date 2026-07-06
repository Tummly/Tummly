using FluentValidation;
using TummlyBackend.DTOs.HelpCentre;

namespace TummlyBackend.Validators
{
    public class SupportReplyDtoValidator : AbstractValidator<SupportReplyDto>
    {
        public SupportReplyDtoValidator()
        {
            RuleFor(x => x.Body)
                .NotEmpty()
                .MaximumLength(5000);
        }
    }
}

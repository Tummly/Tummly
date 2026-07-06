using FluentValidation;
using TummlyBackend.DTOs.HelpCentre;

namespace TummlyBackend.Validators
{
    public class OperatorReplyDtoValidator : AbstractValidator<OperatorReplyDto>
    {
        public OperatorReplyDtoValidator()
        {
            RuleFor(x => x.Body)
                .NotEmpty()
                .MaximumLength(5000);
        }
    }
}

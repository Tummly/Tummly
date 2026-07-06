using FluentValidation;
using TummlyBackend.Models;

namespace TummlyBackend.Validators
{
    public class CreateHelpCentreQueryDtoValidator
        : AbstractValidator<DTOs.HelpCentre.CreateHelpCentreQueryDto>
    {
        public CreateHelpCentreQueryDtoValidator()
        {
            RuleFor(x => x.Topic)
                .NotEmpty()
                .Must(BeValidTopic)
                .WithMessage("Invalid query topic.");

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.SubmitterName)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.SubmitterEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(200);

            RuleFor(x => x.Phone)
                .MaximumLength(30)
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Message)
                .NotEmpty()
                .MaximumLength(5000);
        }

        private static bool BeValidTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return false;
            }

            try
            {
                HelpCentreQueryTopicExtensions.FromSlug(topic);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}

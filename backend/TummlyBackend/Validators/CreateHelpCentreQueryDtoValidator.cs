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
                .WithMessage("Invalid query topic.")
                .When(x => !HasAccountRequestKind(x));

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
                .MaximumLength(5000)
                .When(x => !HasAccountRequestKind(x));

            RuleFor(x => x.AccountRequestKind)
                .Must(BeValidAccountRequestKind)
                .WithMessage("Invalid account request kind.")
                .When(x => !string.IsNullOrWhiteSpace(x.AccountRequestKind));

            RuleFor(x => x.RestaurantId)
                .NotNull()
                .WithMessage("Restaurant id is required for account requests.")
                .When(x => HasAccountRequestKind(x));

            RuleFor(x => x.RestaurantLocationId)
                .Null()
                .WithMessage("Query location must be unset for account requests.")
                .When(x => HasAccountRequestKind(x));
        }

        private static bool HasAccountRequestKind(
            DTOs.HelpCentre.CreateHelpCentreQueryDto dto
        ) =>
            !string.IsNullOrWhiteSpace(dto.AccountRequestKind);

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

        private static bool BeValidAccountRequestKind(string? kind)
        {
            if (string.IsNullOrWhiteSpace(kind))
            {
                return false;
            }

            return HelpCentreAccountRequestKindExtensions.TryParseWireString(
                kind,
                out _
            );
        }
    }
}

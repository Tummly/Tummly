using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class ActivationGate : IActivationGate
    {
        public const string ActivationExpiredMessage =
            "Your 30 day free trial is over";

        public const string ActivationRequiredMessage =
            "Account activation is required before accessing this resource.";

        public ActivationDecision Decide(
            ActivationSubject subject,
            ActivationIntent intent
        )
        {
            if (ActivationState.RequiresActivation(subject))
            {
                return intent == ActivationIntent.ApiAccess
                    ? Block(ActivationReason.Pending, ActivationRequiredMessage)
                    : Allow(ActivationReason.Pending, string.Empty);
            }

            if (ActivationState.IsActivationExpired(subject))
            {
                return Block(ActivationReason.Expired, ActivationExpiredMessage);
            }

            return Allow(ActivationReason.None, string.Empty);
        }

        private static ActivationDecision Allow(
            ActivationReason reason,
            string message
        ) =>
            new(ActivationOutcome.Allow, reason, message);

        private static ActivationDecision Block(
            ActivationReason reason,
            string message
        ) =>
            new(ActivationOutcome.Block, reason, message);
    }
}

using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    internal sealed class AssistantTurnBilling
    {
        public required int RestaurantId { get; init; }

        public required int LocationId { get; init; }

        public string? IdempotencyKey { get; init; }

        public bool LiveAnswerSucceeded { get; private set; }

        public void MarkLiveAnswerSucceeded()
        {
            LiveAnswerSucceeded = true;
        }

        /// <summary>
        /// Only a grounded answer consumes an AI action. Refusal, clarify, gap
        /// and no-clear-driver replies are not answers, even when the provider
        /// call itself succeeded.
        /// </summary>
        public bool ShouldConsume(AssistantMessage message)
            => LiveAnswerSucceeded
                && message.Class == AssistantMessageClass.Grounded;
    }
}

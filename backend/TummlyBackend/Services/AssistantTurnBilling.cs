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

        public bool ShouldConsume(AssistantMessage message)
            => LiveAnswerSucceeded
                && message.Class != AssistantMessageClass.Failure;
    }
}

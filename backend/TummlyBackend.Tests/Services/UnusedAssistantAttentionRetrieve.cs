using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Services
{
    internal sealed class UnusedAssistantAttentionRetrieve : IAssistantAttentionRetrieve
    {
        public Task<AssistantAttentionTurn> PresentAsync(
            AssistantAttentionSurface surface,
            int operatorUserId,
            int locationId,
            string locationName,
            AssistantReportingPeriodDto reportingPeriod,
            CancellationToken cancellationToken = default
        )
            => throw new InvalidOperationException(
                "Attention Retrieve is not used in this test."
            );
    }
}

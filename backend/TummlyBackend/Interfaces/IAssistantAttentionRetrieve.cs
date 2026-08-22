using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;

namespace TummlyBackend.Interfaces
{
    public sealed record AssistantAttentionTurn(
        string Title,
        string Body,
        IReadOnlyList<AssistantActionDto> Actions
    );

    /// <summary>
    /// Presents Home Needs attention, Recommended next step, and Weekly brief
    /// for Attention Retrieve at one Analysis-scope Owned location.
    /// Free call — no AI credit debit.
    /// </summary>
    public interface IAssistantAttentionRetrieve
    {
        Task<AssistantAttentionTurn> PresentAsync(
            AssistantAttentionSurface surface,
            int operatorUserId,
            int locationId,
            string locationName,
            AssistantReportingPeriodDto reportingPeriod,
            CancellationToken cancellationToken = default
        );
    }
}

using System.Text.Json.Nodes;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers;

public static class AssistantDraftCardRefresh
{
    // Keep one current card per object, directly below the latest confirmation.
    // Previous messages retain their text, without a stale preview or review button.
    public static void Apply(AssistantConversation conversation, AssistantMessage update)
    {
        if (update.Class != AssistantMessageClass.Grounded) return;
        var next = AssistantMessagePresentationJson.ParseDto(update.PresentationJson)?.DraftPreview;
        if (next == null) return;
        var previous = conversation.Messages
            .Where(m => m.Role == AssistantMessageRole.Assistant && m.Class == AssistantMessageClass.Grounded)
            .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
            .Where(m => Matches(AssistantMessagePresentationJson.ParseDto(m.PresentationJson)?.DraftPreview, next))
            .ToList();
        if (previous.Count == 0) return;

        if (next.Kind != "campaign" || update.Body.Contains("- **Location:**", StringComparison.Ordinal))
        {
            var prior = AssistantMessagePresentationJson.ParseDto(previous[0].PresentationJson)!.DraftPreview!;
            var changes = new List<string>();
            void Changed(string label, string? oldValue, string? newValue)
            {
                if (oldValue != newValue) changes.Add($"{label}: {newValue ?? "None"}");
            }
            Changed("Audience", prior.AudienceLabel, next.AudienceLabel);
            Changed("Channel", prior.Channel, next.Channel);
            Changed("Subject", prior.Subject, next.Subject);
            Changed("Conditions", prior.ConditionsSummary, next.ConditionsSummary);
            Changed("Validity", prior.ValidityLabel, next.ValidityLabel);
            Changed("Timing", prior.TimingLabel, next.TimingLabel);
            Changed("Name", prior.Name, next.Name);
            Changed("Title", prior.Title, next.Title);
            if (prior.MessageBody != next.MessageBody) changes.Add("Message updated. See the Draft card.");
            update.Body = AssistantCampaignDraftPersistCopy.ChangesBody(changes);
        }

        foreach (var priorMessage in previous) RemovePreview(priorMessage);
    }

    private static bool Matches(AssistantDraftPreviewDto? prior, AssistantDraftPreviewDto next)
        => prior != null && prior.Kind == next.Kind && (next.Kind switch
        {
            "campaign" => next.CampaignId.HasValue && prior.CampaignId == next.CampaignId,
            "offer" => next.OfferId.HasValue && prior.OfferId == next.OfferId,
            "recovery" => next.FeedbackId.HasValue && prior.FeedbackId == next.FeedbackId && prior.Intent == next.Intent,
            _ => false,
        });

    private static void RemovePreview(AssistantMessage message)
    {
        var presentation = JsonNode.Parse(message.PresentationJson!)!.AsObject();
        var kind = presentation["draftPreview"]?["kind"]?.GetValue<string>();
        presentation.Remove("draftPreview");
        message.PresentationJson = presentation.Count == 0 ? null : presentation.ToJsonString();
        var reviewType = kind switch { "campaign" => "review-campaign", "offer" => "review-offer", "recovery" => "open-recovery", _ => "" };
        message.ActionsJson = AssistantAnalysisScope.SerializeActions(
            AssistantAnalysisScope.ParseActions(message.ActionsJson).Where(a => a.Type != reviewType).ToList());
    }
}

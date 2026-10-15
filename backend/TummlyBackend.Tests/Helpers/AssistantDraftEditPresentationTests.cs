using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Tests.Helpers;

public class AssistantDraftEditPresentationTests
{
    [Fact]
    public void EditReply_DoesNotRepeatTheFullDraftSummary()
    {
        var body = AssistantCampaignDraftPersistCopy.UpdatedBody(
            "Main", "Email", "Mohamed and Sophie only", 1, "Thank guests", matchedCount: 2);
        Assert.Contains("Mohamed and Sophie only", body);
        Assert.Contains("2 matched", body);
        Assert.Contains("1 Email-eligible", body);
        Assert.DoesNotContain("**Location:**", body);
        Assert.DoesNotContain("**Name:**", body);
        Assert.DoesNotContain("**Offer:**", body);
    }

    [Fact]
    public void SameDraft_ShowsCurrentCardOnLatestReply_WithoutKeepingAnOldCard()
    {
        var original = Message(42, "Original copy");
        var other = Message(99, "Other Draft");
        var conversation = new AssistantConversation { Messages = [original, other] };
        var update = Message(42, "Shorter copy");
        update.Body = "Subject updated.";
        update.ActionsJson = AssistantAnalysisScope.SerializeActions([new AssistantActionDto { Type = "review-campaign", Label = "Edit Draft", CampaignId = 42 }]);

        AssistantDraftCardRefresh.Apply(conversation, update);

        Assert.Null(AssistantMessagePresentationJson.ParseDto(original.PresentationJson)?.DraftPreview);
        Assert.Equal("Other Draft", AssistantMessagePresentationJson.ParseDto(other.PresentationJson)?.DraftPreview?.MessageBody);
        Assert.Equal("Shorter copy", AssistantMessagePresentationJson.ParseDto(update.PresentationJson)?.DraftPreview?.MessageBody);
        Assert.Single(AssistantAnalysisScope.ParseActions(update.ActionsJson));
        Assert.Equal("Subject updated.", update.Body);
    }

    [Fact]
    public void FailedEdit_KeepsOriginalCard()
    {
        var original = Message(42, "Original copy");
        var conversation = new AssistantConversation { Messages = [original] };
        AssistantDraftCardRefresh.Apply(conversation, new AssistantMessage { Class = AssistantMessageClass.Failure });
        Assert.Equal("Original copy", AssistantMessagePresentationJson.ParseDto(original.PresentationJson)?.DraftPreview?.MessageBody);
    }

    private static AssistantMessage Message(int id, string copy) => new()
    {
        Role = AssistantMessageRole.Assistant,
        Class = AssistantMessageClass.Grounded,
        PresentationJson = AssistantMessagePresentationJson.Serialize(
            AssistantAnswerPresentationShape.ForDraftPreview(
                AssistantDraftPreviewFactory.Campaign(id, "Draft", "Selected guests", "email", "Hello", copy))),
    };
}

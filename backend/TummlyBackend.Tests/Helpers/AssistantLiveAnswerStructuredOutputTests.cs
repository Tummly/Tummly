using System.Text.Json.Nodes;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantLiveAnswerStructuredOutputTests
    {
        [Fact]
        public void BuildSystemPrompt_RequiresGroundedMarkdownAllowList()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Grounded body formatting uses this Markdown allow-list only",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Use headings for distinct sections and bold for short",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "clarify bodies must be plain text",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "The server owns Gap turns",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Do not put Markdown in title",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSystemPrompt_RequiresConversationTitleOmitList()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Emit conversationTitle with assistantTask",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Omit Owned location, Reporting period",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Do not copy title",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSchema_RequiresConversationTitle()
        {
            var schema = AssistantLiveAnswerStructuredOutput.BuildSchema();
            var required = schema["required"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();

            Assert.Contains("conversationTitle", required);
            Assert.Contains("assistantTask", required);
        }

        [Fact]
        public void BuildSystemPrompt_DoesNotRefuseLegalCreateCampaignDraftAsMutate()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Legal Create Campaign Draft, Create Campaign with Offer, Offer path",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "are not Mutate refusals",
                prompt,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "Mutate asks (create, send, or change records) are a refusal",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSystemPrompt_EmptyEvidenceTellsOperatorToChangeScope()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Change Scope to pick another Owned location or Reporting period",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "send a more specific ask",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildSystemPrompt_TreatsHistoryAsContextOnly()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "Prior turns are chat history for reference only",
                prompt,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void BuildRequestJson_WithoutHistory_SendsSystemThenUserPayload()
        {
            var input = new AssistantLiveAnswerInput(
                "Summarise feedback",
                "Camden",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty
            );

            var request = AssistantLiveAnswerStructuredOutput.BuildRequestJson(
                "gpt-4o-mini",
                input,
                "2026-08-16"
            );
            var messages = JsonNode.Parse(request)!["messages"]!.AsArray();

            Assert.Equal(2, messages.Count);
            Assert.Equal("system", messages[0]!["role"]!.GetValue<string>());
            Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
        }

        [Fact]
        public void BuildRequestJson_WithHistory_InsertsTurnsBetweenSystemAndUserPayload()
        {
            var history = new[]
            {
                new AssistantLiveAnswerHistoryTurn(
                    AssistantMessageRole.User,
                    "How did offers do?"
                ),
                new AssistantLiveAnswerHistoryTurn(
                    AssistantMessageRole.Assistant,
                    "Two offers ran."
                ),
            };
            var input = new AssistantLiveAnswerInput(
                "Make it 25% instead",
                "Camden",
                "the last 7 days",
                AssistantRetrievedEvidence.Empty,
                History: history
            );

            var request = AssistantLiveAnswerStructuredOutput.BuildRequestJson(
                "gpt-4o-mini",
                input,
                "2026-08-16"
            );
            var messages = JsonNode.Parse(request)!["messages"]!.AsArray();

            Assert.Equal(4, messages.Count);
            Assert.Equal(
                "system",
                messages[0]!["role"]!.GetValue<string>()
            );
            Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
            Assert.Equal(
                "How did offers do?",
                messages[1]!["content"]!.GetValue<string>()
            );
            Assert.Equal(
                "assistant",
                messages[2]!["role"]!.GetValue<string>()
            );
            Assert.Equal(
                "Two offers ran.",
                messages[2]!["content"]!.GetValue<string>()
            );
            Assert.Equal("user", messages[3]!["role"]!.GetValue<string>());
            var payload = JsonNode.Parse(messages[3]!["content"]!.GetValue<string>())!;
            Assert.Equal(
                "Make it 25% instead",
                payload["userMessage"]!.GetValue<string>()
            );
        }

        [Fact]
        public void BuildSchema_IncludesNullableOfferTermsObject()
        {
            var schema = AssistantLiveAnswerStructuredOutput.BuildSchema();
            var required = schema["required"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();

            Assert.Contains("offerTerms", required);

            var offerTerms = schema["properties"]!["offerTerms"]!
                ["anyOf"]![0]!.AsObject();
            Assert.Equal("object", offerTerms["type"]!.GetValue<string>());

            var termsRequired = offerTerms["required"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();
            foreach (var name in new[]
                     {
                         "offerType",
                         "discountPercentage",
                         "discountAmount",
                         "freeItemText",
                         "purchaseRequirement",
                         "minimumSpend",
                         "replacementItemText",
                         "validity",
                         "expiryDate",
                         "placement"
                     })
            {
                Assert.Contains(name, termsRequired);
            }

            var offerTypeValues = offerTerms["properties"]!["offerType"]!
                ["anyOf"]![0]!["enum"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();
            Assert.Equal(
                new[]
                {
                    "percentage_discount",
                    "fixed_discount",
                    "free_item",
                    "replacement_item"
                },
                offerTypeValues
            );
        }

        [Fact]
        public void BuildSystemPrompt_ExtractsOfferTermsOnLegalCreateTasksOnly()
        {
            var prompt = AssistantLiveAnswerStructuredOutput.BuildSystemPrompt(
                "2026-08-16"
            );

            Assert.Contains(
                "When assistantTask is offer-path, also emit offerTerms",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Emit null offerTerms for every other task",
                prompt,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "The server validates every term",
                prompt,
                StringComparison.Ordinal
            );
        }

        private static string ContentWithOfferTerms(string? offerTermsJson)
        {
            var terms = offerTermsJson ?? "null";
            return $$"""
                {"answerClass":"grounded","title":"T","body":"B","actions":[],"assistantTask":"offer-path","conversationTitle":null,"offerTerms":{{terms}}}
                """;
        }

        private static AssistantLiveAnswerResult? ParseContent(
            string content,
            out bool invalidOutput
        )
        {
            invalidOutput = false;
            AssistantLiveAnswerStructuredOutput.TryParseModelContent(
                content,
                AssistantRetrievedEvidence.Empty,
                "Create an offer",
                out var result,
                out invalidOutput
            );
            return result;
        }

        [Fact]
        public void TryParseModelContent_MapsExtractedTermsOntoCreateShape()
        {
            var content = ContentWithOfferTerms(
                """
                {"offerType":"free_item","discountPercentage":null,"discountAmount":null,"freeItemText":"dessert","purchaseRequirement":"with_any_purchase","minimumSpend":null,"replacementItemText":null,"validity":"choose_expiry_date","expiryDate":"2026-09-30","placement":"guest_form_thank_you"}
                """
            );

            var result = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(
                ParseContent(content, out var invalidOutput)
            );
            Assert.False(invalidOutput);

            var terms = result.OfferTerms;
            Assert.NotNull(terms);
            Assert.Equal("free_item", terms!.OfferType);
            Assert.Equal("dessert", terms.FreeItemText);
            Assert.Equal("with_any_purchase", terms.PurchaseRequirement);
            Assert.Equal("choose_expiry_date", terms.Validity);
            Assert.Equal("2026-09-30", terms.ExpiryDate);
            Assert.True(terms.WantsAttach);
            Assert.Equal(
                AssistantOfferPathTermsState.PlacementGuestFormThankYou,
                terms.Placement
            );
            Assert.True(AssistantOfferPathTerms.IsComplete(terms));
        }

        [Fact]
        public void TryParseModelContent_NullOfferTermsForRetrieveTasks()
        {
            var content =
                """{"answerClass":"clarify","title":null,"body":"Which one?","actions":[],"assistantTask":"retrieve","conversationTitle":null,"offerTerms":null}""";

            var result = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(
                ParseContent(content, out _)
            );

            Assert.Null(result.OfferTerms);
        }

        [Fact]
        public void TryParseModelContent_IgnoresUnknownWireValuesInOfferTerms()
        {
            var content = ContentWithOfferTerms(
                """{"offerType":"triple_discount","validity":"forever"}"""
            );

            var result = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(
                ParseContent(content, out _)
            );

            Assert.Null(result.OfferTerms);
        }
    }
}

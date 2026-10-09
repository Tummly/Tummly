using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Scripted Azure rounds. Each case is one operator prompt type. The stub
    /// never calls a live key. Asserts the provider keeps the model body,
    /// allows one corrective tool wave, and can target an assistant deployment.
    /// </summary>
    public class AzureOpenAIAssistantLiveAnswerProviderPromptSuiteTests
    {
        [Fact]
        public async Task DataQuestion_ToolThenAnswer_KeepsModelBody()
        {
            var body = "12 guests joined Camden this week. That is the new-guest count on Home, so a welcome offer is the useful next step.";
            var handler = new ScriptedHandler(
                ToolCall("call_home", AssistantRetrieveToolCatalog.ReadHomeKpis),
                Answer("Guests joined", body)
            );

            var result = await CompleteAsync(
                handler,
                "How many guests joined this week?"
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(result);
            Assert.Equal(body, succeeded.Body);
            Assert.Equal(AssistantTask.Retrieve, succeeded.AssistantTask);
            Assert.Equal(
                [AssistantRetrieveToolCatalog.ReadHomeKpis],
                handler.ExecutedToolNames
            );
            Assert.Equal(2, handler.RequestCount);
            AssertPromptVoice(handler);
            Record("data", "How many guests joined this week?", body, "read_home_kpis then model body");
        }

        [Fact]
        public async Task Explanation_ToolThenInterpretation_KeepsModelBody()
        {
            var body = "QR scans fell from 40 to 18. The drop is in the scanned count itself; I cannot see a cause beyond that, so check whether the table QR is still on display.";
            var handler = new ScriptedHandler(
                ToolCall("call_capture", AssistantRetrieveToolCatalog.ReadCapturePerformance),
                Answer("Scans dropped", body)
            );

            var result = await CompleteAsync(handler, "Why did scans drop?");

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(result);
            Assert.Equal(body, succeeded.Body);
            Assert.Equal(
                [AssistantRetrieveToolCatalog.ReadCapturePerformance],
                handler.ExecutedToolNames
            );
            Record("explanation", "Why did scans drop?", body, "read_capture_performance then interpretation");
        }

        [Fact]
        public async Task ProductHelp_NoTools_KeepsModelBody()
        {
            var body = "I can read Feedback, offers, Campaigns, Capture, and guests, and I can draft a Campaign or an Offer when you ask. Tell me which location you want to look at first.";
            var handler = new ScriptedHandler(
                Answer("How I can help", body)
            );

            var result = await CompleteAsync(handler, "What can you help me with?");

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(result);
            Assert.Equal(body, succeeded.Body);
            Assert.Empty(handler.ExecutedToolNames);
            Assert.Equal(1, handler.RequestCount);
            Record("product help", "What can you help me with?", body, "no tools; model body kept");
        }

        [Fact]
        public async Task CreateFollowUp_KeepsModelProseAndTask()
        {
            var body = "I can draft an Email Campaign for guests who have not visited lately. I will not send it. Say the location if you want me to save the draft.";
            var handler = new ScriptedHandler(
                ToolCall("call_guests", AssistantRetrieveToolCatalog.ReadGuests),
                Answer(
                    "Win-back draft",
                    body,
                    assistantTask: AssistantTask.CreateCampaignDraft
                )
            );

            var result = await CompleteAsync(
                handler,
                "Draft an email campaign for guests who have not visited lately"
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(result);
            Assert.Equal(body, succeeded.Body);
            Assert.Equal(AssistantTask.CreateCampaignDraft, succeeded.AssistantTask);
            Assert.Equal(
                [AssistantRetrieveToolCatalog.ReadGuests],
                handler.ExecutedToolNames
            );
            Record(
                "create follow-up",
                "Draft an email campaign for guests who have not visited lately",
                body,
                "read_guests; task stays create-campaign-draft"
            );
        }

        [Fact]
        public async Task VagueHello_ReturnsClarify()
        {
            var handler = new ScriptedHandler(
                Answer(
                    null,
                    "What do you want to know about Feedback, offers, Campaigns, or guests?",
                    answerClass: "clarify"
                )
            );

            var result = await CompleteAsync(handler, "hello");

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(result);
            Assert.Equal(AssistantMessageClass.Clarify, succeeded.Class);
            Assert.Contains("Feedback", succeeded.Body, StringComparison.Ordinal);
            Assert.Empty(handler.ExecutedToolNames);
            Record("vague", "hello", succeeded.Body, "clarify, no tools");
        }

        [Fact]
        public async Task SecondToolWave_ExecutesBothLookups_ThenKeepsFinalBody()
        {
            var body = "Feedback is quiet this week, and 9 guests joined. Start with those new guests rather than a recovery draft.";
            var handler = new ScriptedHandler(
                ToolCall("call_feedback", AssistantRetrieveToolCatalog.ReadFeedbackSummary),
                ToolCall("call_home", AssistantRetrieveToolCatalog.ReadHomeKpis),
                Answer("Feedback and new guests", body)
            );

            var result = await CompleteAsync(
                handler,
                "How many guests joined this week, and how is feedback?"
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(result);
            Assert.Equal(body, succeeded.Body);
            Assert.Equal(
                [
                    AssistantRetrieveToolCatalog.ReadFeedbackSummary,
                    AssistantRetrieveToolCatalog.ReadHomeKpis,
                ],
                handler.ExecutedToolNames
            );
            Assert.Equal(3, handler.RequestCount);
            Assert.Contains("\"response_format\"", handler.RequestBodies[^1]);
            Assert.DoesNotContain("\"tools\"", handler.RequestBodies[^1]);
            Record(
                "second tool wave",
                "How many guests joined this week, and how is feedback?",
                body,
                "read_feedback_summary then read_home_kpis then structured answer"
            );
        }

        [Fact]
        public async Task OfferExpiryFollowUp_WithoutTools_ReadsHistoryAndKeepsTerms()
        {
            var payload = JsonSerializer.Serialize(
                new Dictionary<string, object?>
                {
                    ["answerClass"] = "grounded",
                    ["title"] = "Lunch offer",
                    ["body"] = "The 25% lunch offer ends two weeks after a guest gets it.",
                    ["actions"] = Array.Empty<object>(),
                    ["assistantTask"] = AssistantTask.OfferPath,
                    ["conversationTitle"] = "Lunch offer",
                    ["offerTerms"] = new Dictionary<string, object?>
                    {
                        ["offerType"] = "percentage_discount",
                        ["discountPercentage"] = 25,
                        ["discountAmount"] = null,
                        ["freeItemText"] = null,
                        ["purchaseRequirement"] = null,
                        ["minimumSpend"] = null,
                        ["replacementItemText"] = null,
                        ["validity"] = "14_days_after_issue",
                        ["expiryDate"] = null,
                        ["placement"] = null,
                    },
                }
            );
            var content = JsonSerializer.Serialize(payload);
            var handler = new ScriptedHandler(
                $$"""
                {
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": {{content}}
                      }
                    }
                  ]
                }
                """
            );
            var httpClient = new HttpClient(handler);
            var provider = new AzureOpenAIAssistantLiveAnswerProvider(
                new StubHttpClientFactory(httpClient),
                Options.Create(
                    new FeedbackClassificationSettings
                    {
                        Provider = "AzureOpenAI",
                        Endpoint = "https://tummly-test.openai.azure.com/",
                        ApiKey = "test-key",
                        DeploymentName = "gpt-4o-mini",
                        ApiVersion = "2024-08-01-preview",
                        PromptSchemaVersion = "2026-07-18",
                    }
                ),
                NullLogger<AzureOpenAIAssistantLiveAnswerProvider>.Instance
            );

            var result = await provider.CompleteAsync(
                new AssistantLiveAnswerInput(
                    "next Friday",
                    "Camden",
                    "this week",
                    AssistantRetrievedEvidence.Empty,
                    History:
                    [
                        new AssistantLiveAnswerHistoryTurn(
                            AssistantMessageRole.User,
                            "Create a 25% off lunch offer"
                        ),
                        new AssistantLiveAnswerHistoryTurn(
                            AssistantMessageRole.Assistant,
                            "When should the offer end? Send a date, or how many days after a guest gets it."
                        ),
                    ]
                )
            );

            var succeeded = Assert.IsType<AssistantLiveAnswerResult.Succeeded>(result);
            Assert.Equal(AssistantTask.OfferPath, succeeded.AssistantTask);
            Assert.Equal("14_days_after_issue", succeeded.OfferTerms?.Validity);
            Assert.Contains("25% lunch offer", succeeded.Body, StringComparison.Ordinal);
            Assert.Equal(1, handler.RequestCount);
            Assert.Contains("Create a 25% off lunch offer", handler.RequestBodies[0]);
            Assert.Contains("When should the offer end?", handler.RequestBodies[0]);
            Assert.Contains("next Friday", handler.RequestBodies[0]);
            Assert.Contains("\"response_format\"", handler.RequestBodies[0]);
            Record(
                "offer expiry follow-up",
                "next Friday",
                succeeded.Body,
                "history kept the 25% lunch offer; no retrieve tools"
            );
        }

        [Fact]
        public async Task AssistantDeploymentName_OverridesSharedDeployment()
        {
            var handler = new ScriptedHandler(
                Answer("How I can help", "I can help with Feedback and drafts.")
            );

            await CompleteAsync(
                handler,
                "What can you help me with?",
                assistantDeploymentName: "gpt-5-mini"
            );

            Assert.Contains(
                "/openai/deployments/gpt-5-mini/chat/completions",
                handler.RequestUris[0],
                StringComparison.Ordinal
            );
            Assert.Contains("\"model\":\"gpt-5-mini\"", handler.RequestBodies[0]);
        }

        private static readonly object LogGate = new();

        private static void Record(string kind, string prompt, string body, string notes)
        {
            const string path = "/opt/cursor/artifacts/assistant-prompt-suite.log";
            var directory = Path.GetDirectoryName(path);
            if (directory is null || !Directory.Exists(directory))
            {
                return;
            }

            lock (LogGate)
            {
                File.AppendAllText(
                    path,
                    $"""
                    [{kind}] {prompt}
                    notes: {notes}
                    answer: {body}

                    """
                );
            }
        }

        private static void AssertPromptVoice(ScriptedHandler handler)
        {
            var system = handler.RequestBodies[0];
            Assert.Contains(
                "explain what the retrieved numbers mean",
                system,
                StringComparison.Ordinal
            );
            Assert.Contains("\"max_completion_tokens\":4096", system);
            Assert.DoesNotContain(
                "answer only what was asked",
                system,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static async Task<AssistantLiveAnswerResult> CompleteAsync(
            ScriptedHandler handler,
            string userMessage,
            string assistantDeploymentName = ""
        )
        {
            var httpClient = new HttpClient(handler);
            var settings = Options.Create(
                new FeedbackClassificationSettings
                {
                    Provider = "AzureOpenAI",
                    Endpoint = "https://tummly-test.openai.azure.com/",
                    ApiKey = "test-key",
                    DeploymentName = "gpt-4o-mini",
                    AssistantDeploymentName = assistantDeploymentName,
                    ApiVersion = "2024-08-01-preview",
                    PromptSchemaVersion = "2026-07-18",
                }
            );
            var provider = new AzureOpenAIAssistantLiveAnswerProvider(
                new StubHttpClientFactory(httpClient),
                settings,
                NullLogger<AzureOpenAIAssistantLiveAnswerProvider>.Instance
            );
            var executed = handler.ExecutedToolNames;
            return await provider.CompleteAsync(
                new AssistantLiveAnswerInput(
                    userMessage,
                    "Camden",
                    "this week",
                    AssistantRetrievedEvidence.Empty,
                    ExecuteRetrieveTools: (calls, _) =>
                    {
                        foreach (var call in calls)
                        {
                            executed.Add(call.Name);
                        }

                        IReadOnlyList<AssistantToolCallResult> results = calls
                            .Select(call => new AssistantToolCallResult(
                                call.Id,
                                call.Name,
                                """{"note":"stub"}"""
                            ))
                            .ToList();
                        return Task.FromResult(results);
                    }
                )
            );
        }

        private static string ToolCall(string id, string name)
            => $$"""
                {
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": null,
                        "tool_calls": [
                          {
                            "id": "{{id}}",
                            "type": "function",
                            "function": { "name": "{{name}}", "arguments": "{}" }
                          }
                        ]
                      }
                    }
                  ]
                }
                """;

        private static string Answer(
            string? title,
            string body,
            string answerClass = "grounded",
            string assistantTask = AssistantTask.Retrieve
        )
        {
            var payload = JsonSerializer.Serialize(
                new Dictionary<string, object?>
                {
                    ["answerClass"] = answerClass,
                    ["title"] = title,
                    ["body"] = body,
                    ["actions"] = Array.Empty<object>(),
                    ["assistantTask"] = assistantTask,
                    ["conversationTitle"] = title,
                    ["offerTerms"] = null,
                }
            );
            var content = JsonSerializer.Serialize(payload);
            return $$"""
                {
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": {{content}}
                      }
                    }
                  ]
                }
                """;
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => httpClient;
        }

        private sealed class ScriptedHandler : HttpMessageHandler
        {
            private readonly Queue<string> _responses;

            public ScriptedHandler(params string[] responses)
            {
                _responses = new Queue<string>(responses);
            }

            public int RequestCount { get; private set; }

            public List<string> RequestBodies { get; } = [];

            public List<string> RequestUris { get; } = [];

            public List<string> ExecutedToolNames { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                RequestCount += 1;
                RequestUris.Add(request.RequestUri?.ToString() ?? string.Empty);
                RequestBodies.Add(
                    request.Content is null
                        ? string.Empty
                        : await request.Content.ReadAsStringAsync(cancellationToken)
                );
                var json = _responses.Count > 0 ? _responses.Dequeue() : "{}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}

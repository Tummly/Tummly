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
    public class AzureOpenAIFeedbackClassificationProviderTests
    {
        [Fact]
        public void StructuredOutputsRequest_documents_schema_contract()
        {
            var json = FeedbackClassificationStructuredOutput.BuildRequestJson(
                deploymentName: "gpt-4o-mini",
                comment: "The chips were cold.",
                promptSchemaVersion: "2026-07-18"
            );

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.Equal("gpt-4o-mini", root.GetProperty("model").GetString());

            var responseFormat = root.GetProperty("response_format");
            Assert.Equal("json_schema", responseFormat.GetProperty("type").GetString());

            var jsonSchema = responseFormat.GetProperty("json_schema");
            Assert.Equal(
                FeedbackClassificationStructuredOutput.SchemaName,
                jsonSchema.GetProperty("name").GetString()
            );
            Assert.True(jsonSchema.GetProperty("strict").GetBoolean());

            var schema = jsonSchema.GetProperty("schema");
            Assert.Equal("object", schema.GetProperty("type").GetString());
            Assert.False(schema.GetProperty("additionalProperties").GetBoolean());

            var required = schema.GetProperty("required")
                .EnumerateArray()
                .Select(element => element.GetString())
                .ToHashSet();
            Assert.Contains("outcome", required);
            Assert.Contains("sentiment", required);
            Assert.Contains("detectedTags", required);

            var outcomeEnum = schema
                .GetProperty("properties")
                .GetProperty("outcome")
                .GetProperty("enum")
                .EnumerateArray()
                .Select(element => element.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            Assert.Equal(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "classified",
                    "unsupported_language"
                },
                outcomeEnum
            );

            var sentimentEnum = schema
                .GetProperty("properties")
                .GetProperty("sentiment")
                .GetProperty("anyOf")[0]
                .GetProperty("enum")
                .EnumerateArray()
                .Select(element => element.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            Assert.Equal(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "positive",
                    "neutral",
                    "negative"
                },
                sentimentEnum
            );

            var tagEnum = schema
                .GetProperty("properties")
                .GetProperty("detectedTags")
                .GetProperty("anyOf")[0]
                .GetProperty("items")
                .GetProperty("enum")
                .EnumerateArray()
                .Select(element => element.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
            Assert.Contains("FoodQuality", tagEnum);
            Assert.Contains("Other", tagEnum);
            Assert.Equal(10, tagEnum.Count);

            var systemContent = root
                .GetProperty("messages")[0]
                .GetProperty("content")
                .GetString();
            Assert.Contains("2026-07-18", systemContent);
            Assert.Contains("UK hospitality", systemContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Other", systemContent);
        }

        [Fact]
        public async Task ClassifyAsync_retries_http_timeout_then_succeeds()
        {
            var handler = new SequenceHttpMessageHandler(
                () => throw new TaskCanceledException("timeout"),
                () => JsonResponse(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"outcome\":\"classified\",\"sentiment\":\"positive\",\"detectedTags\":[]}"
                          }
                        }
                      ]
                    }
                    """
                )
            );

            var provider = CreateProvider(handler, maxAttempts: 3, backoffMs: 0);

            var result = await provider.ClassifyAsync("Great service");

            var succeeded = Assert.IsType<FeedbackClassificationResult.Succeeded>(result);
            Assert.Equal(FeedbackSentiment.Positive, succeeded.Sentiment);
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public async Task ClassifyAsync_maps_classified_structured_output_to_Succeeded()
        {
            var handler = new SequenceHttpMessageHandler(
                () => JsonResponse(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"outcome\":\"classified\",\"sentiment\":\"negative\",\"detectedTags\":[\"FoodQuality\",\"WaitTime\"]}"
                          }
                        }
                      ]
                    }
                    """
                )
            );

            var provider = CreateProvider(handler);

            var result = await provider.ClassifyAsync("Cold chips and a long wait.");

            var succeeded = Assert.IsType<FeedbackClassificationResult.Succeeded>(result);
            Assert.Equal(FeedbackSentiment.Negative, succeeded.Sentiment);
            Assert.Equal(
                new[] { DetectedTag.FoodQuality, DetectedTag.WaitTime },
                succeeded.DetectedTags
            );
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task ClassifyAsync_unsupported_language_returns_Failed_without_retry()
        {
            var handler = new SequenceHttpMessageHandler(
                () => JsonResponse(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"outcome\":\"unsupported_language\",\"sentiment\":null,\"detectedTags\":null}"
                          }
                        }
                      ]
                    }
                    """
                ),
                () => JsonResponse(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"outcome\":\"classified\",\"sentiment\":\"neutral\",\"detectedTags\":[]}"
                          }
                        }
                      ]
                    }
                    """
                )
            );

            var provider = CreateProvider(handler);

            var result = await provider.ClassifyAsync("Bonjour, le service etait terrible.");

            var failed = Assert.IsType<FeedbackClassificationResult.Failed>(result);
            Assert.False(failed.Retryable);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task ClassifyAsync_retries_transient_errors_then_succeeds()
        {
            var handler = new SequenceHttpMessageHandler(
                () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                () => JsonResponse(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"outcome\":\"classified\",\"sentiment\":\"neutral\",\"detectedTags\":[]}"
                          }
                        }
                      ]
                    }
                    """
                )
            );

            var provider = CreateProvider(handler, maxAttempts: 3, backoffMs: 0);

            var result = await provider.ClassifyAsync("Fine thanks");

            var succeeded = Assert.IsType<FeedbackClassificationResult.Succeeded>(result);
            Assert.Equal(FeedbackSentiment.Neutral, succeeded.Sentiment);
            Assert.Empty(succeeded.DetectedTags);
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public async Task ClassifyAsync_Other_combined_with_tags_is_invalid_and_retries_then_Failed()
        {
            static HttpResponseMessage InvalidOtherCombo()
                => JsonResponse(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"outcome\":\"classified\",\"sentiment\":\"negative\",\"detectedTags\":[\"Other\",\"Service\"]}"
                          }
                        }
                      ]
                    }
                    """
                );

            var handler = new SequenceHttpMessageHandler(
                InvalidOtherCombo,
                InvalidOtherCombo,
                InvalidOtherCombo
            );
            var provider = CreateProvider(handler, maxAttempts: 3, backoffMs: 0);

            var result = await provider.ClassifyAsync("Something odd went wrong.");

            var failed = Assert.IsType<FeedbackClassificationResult.Failed>(result);
            Assert.False(failed.Retryable);
            Assert.Equal(3, handler.RequestCount);
        }

        [Fact]
        public async Task ClassifyAsync_NotFound_returns_retryable_Failed()
        {
            var handler = new SequenceHttpMessageHandler(
                () => new HttpResponseMessage(HttpStatusCode.NotFound)
            );
            var provider = CreateProvider(handler, maxAttempts: 3, backoffMs: 0);

            var result = await provider.ClassifyAsync("Chips were cold");

            var failed = Assert.IsType<FeedbackClassificationResult.Failed>(result);
            Assert.True(failed.Retryable);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task ClassifyAsync_Unauthorized_returns_retryable_Failed()
        {
            var handler = new SequenceHttpMessageHandler(
                () => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            );
            var provider = CreateProvider(handler, maxAttempts: 1, backoffMs: 0);

            var result = await provider.ClassifyAsync("Chips were cold");

            var failed = Assert.IsType<FeedbackClassificationResult.Failed>(result);
            Assert.True(failed.Retryable);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task ClassifyAsync_sends_api_key_and_deployment_chat_completions_path()
        {
            var handler = new SequenceHttpMessageHandler(
                () => JsonResponse(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"outcome\":\"classified\",\"sentiment\":\"positive\",\"detectedTags\":[]}"
                          }
                        }
                      ]
                    }
                    """
                )
            );

            var provider = CreateProvider(handler);

            await provider.ClassifyAsync("Lovely meal");

            Assert.NotNull(handler.LastRequest);
            Assert.Equal(
                "https://tummly-test.openai.azure.com/openai/deployments/gpt-4o-mini/chat/completions?api-version=2024-08-01-preview",
                handler.LastRequest!.RequestUri!.ToString()
            );
            Assert.True(
                handler.LastRequest.Headers.TryGetValues("api-key", out var keys)
            );
            Assert.Equal("test-key", Assert.Single(keys!));
        }

        private static AzureOpenAIFeedbackClassificationProvider CreateProvider(
            SequenceHttpMessageHandler handler,
            int maxAttempts = 3,
            int backoffMs = 0
        )
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://tummly-test.openai.azure.com/")
            };

            var settings = Options.Create(
                new FeedbackClassificationSettings
                {
                    Provider = "AzureOpenAI",
                    Endpoint = "https://tummly-test.openai.azure.com/",
                    ApiKey = "test-key",
                    DeploymentName = "gpt-4o-mini",
                    ApiVersion = "2024-08-01-preview",
                    Region = "uksouth",
                    PromptSchemaVersion = "2026-07-18",
                    MaxAttempts = maxAttempts,
                    InitialBackoffMilliseconds = backoffMs
                }
            );

            return new AzureOpenAIFeedbackClassificationProvider(
                new StubHttpClientFactory(httpClient),
                settings,
                NullLogger<AzureOpenAIFeedbackClassificationProvider>.Instance
            );
        }

        private static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => httpClient;
        }

        private sealed class SequenceHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<Func<HttpResponseMessage>> _responses;

            public SequenceHttpMessageHandler(
                params Func<HttpResponseMessage>[] responses
            )
            {
                _responses = new Queue<Func<HttpResponseMessage>>(responses);
            }

            public int RequestCount { get; private set; }

            public HttpRequestMessage? LastRequest { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                RequestCount += 1;
                LastRequest = request;

                // Force materialisation so tests can inspect the body later if needed.
                if (request.Content is not null)
                {
                    await request.Content.ReadAsStringAsync(cancellationToken);
                }

                if (_responses.Count == 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                return _responses.Dequeue()();
            }
        }
    }
}

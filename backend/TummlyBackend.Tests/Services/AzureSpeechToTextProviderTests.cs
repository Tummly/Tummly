using System.Net;
using System.Net.Http.Headers;
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
    public class AzureSpeechToTextProviderTests
    {
        [Fact]
        public void FastTranscriptionDefinition_documents_en_GB_locale_contract()
        {
            var json = AzureSpeechFastTranscription.BuildDefinitionJson("en-GB");

            using var document = JsonDocument.Parse(json);
            var locales = document.RootElement
                .GetProperty("locales")
                .EnumerateArray()
                .Select(element => element.GetString())
                .ToArray();

            Assert.Equal(new[] { "en-GB" }, locales);
        }

        [Fact]
        public async Task TranscribeAsync_sends_subscription_key_multipart_fast_transcription()
        {
            var handler = new SequenceHttpMessageHandler(
                () => JsonResponse(
                    """
                    {
                      "durationMilliseconds": 1200,
                      "combinedPhrases": [ { "text": "The chips were cold." } ],
                      "phrases": []
                    }
                    """
                )
            );

            var provider = CreateProvider(handler);

            await using var audio = new MemoryStream(Encoding.UTF8.GetBytes("fake-webm"));
            var result = await provider.TranscribeAsync(
                audio,
                "audio/webm",
                CancellationToken.None
            );

            var succeeded = Assert.IsType<SpeechToTextResult.Succeeded>(result);
            Assert.Equal("The chips were cold.", succeeded.Text);

            Assert.NotNull(handler.LastRequest);
            Assert.Equal(
                "https://tummly-speech.cognitiveservices.azure.com/speechtotext/transcriptions:transcribe?api-version=2025-10-15",
                handler.LastRequest!.RequestUri!.ToString()
            );
            Assert.True(
                handler.LastRequest.Headers.TryGetValues(
                    "Ocp-Apim-Subscription-Key",
                    out var keys
                )
            );
            Assert.Equal("test-speech-key", Assert.Single(keys!));

            Assert.True(handler.SawDefinitionPart);
            Assert.True(handler.SawAudioPart);
            Assert.Equal("audio/webm", handler.AudioMediaType);

            using var definition = JsonDocument.Parse(handler.DefinitionJson!);
            Assert.Equal(
                "en-GB",
                definition.RootElement.GetProperty("locales")[0].GetString()
            );
        }

        [Fact]
        public async Task TranscribeAsync_empty_combined_phrases_returns_EmptySpeech()
        {
            var handler = new SequenceHttpMessageHandler(
                () => JsonResponse(
                    """
                    {
                      "durationMilliseconds": 800,
                      "combinedPhrases": [ { "text": "   " } ],
                      "phrases": []
                    }
                    """
                )
            );

            var provider = CreateProvider(handler);

            await using var audio = new MemoryStream([1, 2, 3]);
            var result = await provider.TranscribeAsync(
                audio,
                "audio/webm",
                CancellationToken.None
            );

            Assert.IsType<SpeechToTextResult.EmptySpeech>(result);
        }

        [Fact]
        public async Task TranscribeAsync_provider_error_returns_Failed()
        {
            var handler = new SequenceHttpMessageHandler(
                () => new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        """{"error":{"code":"InvalidRequest","message":"bad audio"}}""",
                        Encoding.UTF8,
                        "application/json"
                    )
                }
            );

            var provider = CreateProvider(handler);

            await using var audio = new MemoryStream([1, 2, 3]);
            var result = await provider.TranscribeAsync(
                audio,
                "audio/webm",
                CancellationToken.None
            );

            Assert.IsType<SpeechToTextResult.Failed>(result);
        }

        [Fact]
        public async Task TranscribeAsync_misconfigured_credentials_returns_Failed_without_http()
        {
            var handler = new SequenceHttpMessageHandler();
            var provider = CreateProvider(
                handler,
                settings =>
                {
                    settings.Endpoint = "";
                    settings.ApiKey = "";
                }
            );

            await using var audio = new MemoryStream([1, 2, 3]);
            var result = await provider.TranscribeAsync(
                audio,
                "audio/webm",
                CancellationToken.None
            );

            Assert.IsType<SpeechToTextResult.Failed>(result);
            Assert.Equal(0, handler.RequestCount);
        }

        private static AzureSpeechToTextProvider CreateProvider(
            SequenceHttpMessageHandler handler,
            Action<SpeechToTextSettings>? configure = null
        )
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(
                    "https://tummly-speech.cognitiveservices.azure.com/"
                )
            };

            var settings = new SpeechToTextSettings
            {
                Provider = "AzureSpeech",
                Endpoint =
                    "https://tummly-speech.cognitiveservices.azure.com/",
                ApiKey = "test-speech-key",
                Locale = "en-GB",
                ApiVersion = "2025-10-15",
                Mode = "FastTranscription"
            };
            configure?.Invoke(settings);

            return new AzureSpeechToTextProvider(
                new StubHttpClientFactory(httpClient),
                Options.Create(settings),
                NullLogger<AzureSpeechToTextProvider>.Instance
            );
        }

        private static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class StubHttpClientFactory(HttpClient httpClient)
            : IHttpClientFactory
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

            public bool SawDefinitionPart { get; private set; }

            public bool SawAudioPart { get; private set; }

            public string? DefinitionJson { get; private set; }

            public string? AudioMediaType { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                RequestCount += 1;
                LastRequest = request;

                if (request.Content is MultipartFormDataContent multipart)
                {
                    foreach (var part in multipart)
                    {
                        var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                        if (name == "definition")
                        {
                            SawDefinitionPart = true;
                            DefinitionJson = await part.ReadAsStringAsync(
                                cancellationToken
                            );
                        }
                        else if (name == "audio")
                        {
                            SawAudioPart = true;
                            AudioMediaType = part.Headers.ContentType?.MediaType;
                            _ = await part.ReadAsByteArrayAsync(cancellationToken);
                        }
                    }
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

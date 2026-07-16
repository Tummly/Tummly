using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Production STT provider: Azure AI Speech fast transcription (ephemeral, no at-rest audio).
    /// </summary>
    public sealed class AzureSpeechToTextProvider : ISpeechToTextProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SpeechToTextSettings _settings;
        private readonly ILogger<AzureSpeechToTextProvider> _logger;

        public AzureSpeechToTextProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<SpeechToTextSettings> settings,
            ILogger<AzureSpeechToTextProvider> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<SpeechToTextResult> TranscribeAsync(
            Stream audio,
            string contentType,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(_settings.Endpoint)
                || string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError(
                    "Azure Speech STT is misconfigured (endpoint or api key)."
                );
                return new SpeechToTextResult.Failed();
            }

            if (!_settings.Mode.Equals(
                    "FastTranscription",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                _logger.LogError(
                    "Unsupported SpeechToText mode '{Mode}'. Only FastTranscription is supported.",
                    _settings.Mode
                );
                return new SpeechToTextResult.Failed();
            }

            try
            {
                // Buffer in-process only — never write guest audio to disk or object storage.
                await using var buffer = new MemoryStream();
                await audio.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;

                if (buffer.Length == 0)
                {
                    return new SpeechToTextResult.EmptySpeech();
                }

                var client = _httpClientFactory.CreateClient(
                    AzureSpeechFastTranscription.HttpClientName
                );

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    AzureSpeechFastTranscription.BuildTranscribeUri(
                        _settings.Endpoint,
                        string.IsNullOrWhiteSpace(_settings.ApiVersion)
                            ? AzureSpeechFastTranscription.DefaultApiVersion
                            : _settings.ApiVersion
                    )
                );

                request.Headers.TryAddWithoutValidation(
                    "Ocp-Apim-Subscription-Key",
                    _settings.ApiKey
                );
                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json")
                );

                var multipart = new MultipartFormDataContent();
                multipart.Add(
                    new StringContent(
                        AzureSpeechFastTranscription.BuildDefinitionJson(
                            string.IsNullOrWhiteSpace(_settings.Locale)
                                ? "en-GB"
                                : _settings.Locale
                        )
                    ),
                    "definition"
                );

                var mediaType = string.IsNullOrWhiteSpace(contentType)
                    ? "application/octet-stream"
                    : contentType.Split(';')[0].Trim();

                var audioContent = new StreamContent(buffer);
                audioContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
                multipart.Add(audioContent, "audio", GuessFileName(mediaType));

                request.Content = multipart;

                using var response = await client.SendAsync(
                    request,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Azure Speech STT failed with {StatusCode}",
                        (int)response.StatusCode
                    );
                    return new SpeechToTextResult.Failed();
                }

                var responseJson = await response.Content.ReadAsStringAsync(
                    cancellationToken
                );

                return AzureSpeechFastTranscription.ToResult(responseJson);
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested
            )
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure Speech STT request failed");
                return new SpeechToTextResult.Failed();
            }
        }

        private static string GuessFileName(string mediaType)
            => mediaType.ToLowerInvariant() switch
            {
                "audio/webm" => "audio.webm",
                "audio/ogg" => "audio.ogg",
                "audio/mp4" => "audio.mp4",
                "audio/mpeg" => "audio.mp3",
                "audio/wav" or "audio/wave" or "audio/x-wav" => "audio.wav",
                _ => "audio.bin"
            };
    }
}

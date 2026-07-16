using System.Text.Json;
using System.Text.Json.Nodes;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Azure AI Speech fast transcription HTTP contract for ephemeral guest STT.
    /// Audio is posted in-memory multipart — never written to Tummly storage.
    /// </summary>
    public static class AzureSpeechFastTranscription
    {
        public const string HttpClientName = "AzureSpeechFastTranscription";

        public const string DefaultApiVersion = "2025-10-15";

        public static string BuildDefinitionJson(string locale)
        {
            var definition = new JsonObject
            {
                ["locales"] = new JsonArray(locale)
            };

            return definition.ToJsonString(
                new JsonSerializerOptions { WriteIndented = false }
            );
        }

        public static Uri BuildTranscribeUri(string endpoint, string apiVersion)
        {
            var baseEndpoint = endpoint.TrimEnd('/') + "/";
            var relative =
                "speechtotext/transcriptions:transcribe"
                + $"?api-version={Uri.EscapeDataString(apiVersion)}";

            return new Uri(new Uri(baseEndpoint), relative);
        }

        public static bool TryParseTranscript(
            string responseJson,
            out string? text,
            out bool emptySpeech
        )
        {
            text = null;
            emptySpeech = false;

            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;

                if (!root.TryGetProperty("combinedPhrases", out var phrases)
                    || phrases.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var parts = new List<string>();

                foreach (var phrase in phrases.EnumerateArray())
                {
                    if (phrase.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (!phrase.TryGetProperty("text", out var textElement)
                        || textElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var phraseText = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(phraseText))
                    {
                        parts.Add(phraseText.Trim());
                    }
                }

                if (parts.Count == 0)
                {
                    emptySpeech = true;
                    text = string.Empty;
                    return true;
                }

                text = string.Join(" ", parts);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static SpeechToTextResult ToResult(
            string responseJson
        )
        {
            if (!TryParseTranscript(responseJson, out var text, out var emptySpeech))
            {
                return new SpeechToTextResult.Failed();
            }

            if (emptySpeech)
            {
                return new SpeechToTextResult.EmptySpeech();
            }

            return new SpeechToTextResult.Succeeded(text!);
        }
    }
}

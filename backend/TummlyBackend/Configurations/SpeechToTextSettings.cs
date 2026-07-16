namespace TummlyBackend.Configurations
{
    /// <summary>
    /// Guest ephemeral STT settings. Credentials stay backend-only.
    /// Production default is Azure AI Speech fast transcription (<c>en-GB</c>).
    /// OpenAI audio transcriptions remain a documented one-vendor alternative
    /// in the phase-1 AI pipeline spec — not the default and not wired in this
    /// settings surface until a dedicated client is added.
    /// </summary>
    public class SpeechToTextSettings
    {
        public const string SectionName = "SpeechToText";

        /// <summary>
        /// <c>AzureSpeech</c> (production default) or <c>Fake</c> (tests/local demos).
        /// OpenAI audio transcriptions are a documented one-vendor alternative
        /// (see phase-1 AI pipeline spec) but are not wired here — do not set
        /// <c>Provider=OpenAI</c> until that client is implemented.
        /// </summary>
        public string Provider { get; set; } = "AzureSpeech";

        /// <summary>
        /// Azure Speech resource endpoint
        /// (e.g. https://{resource}.cognitiveservices.azure.com/).
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>BCP-47 locale for transcription (product default <c>en-GB</c>).</summary>
        public string Locale { get; set; } = "en-GB";

        public string ApiVersion { get; set; } = "2025-10-15";

        /// <summary>
        /// <c>FastTranscription</c> — synchronous, no customer blob / at-rest audio path.
        /// </summary>
        public string Mode { get; set; } = "FastTranscription";
    }
}

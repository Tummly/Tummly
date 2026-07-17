namespace TummlyBackend.Models
{
    /// <summary>
    /// Result of ephemeral guest speech-to-text. Audio is never persisted.
    /// </summary>
    public abstract record SpeechToTextResult
    {
        private SpeechToTextResult()
        {
        }

        public sealed record Succeeded(string Text) : SpeechToTextResult;

        public sealed record EmptySpeech : SpeechToTextResult;

        public sealed record Failed : SpeechToTextResult;
    }
}

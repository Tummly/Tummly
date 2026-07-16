using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Fakeable provider that transcribes ephemeral guest audio.
    /// Phase 1 default is a fake; Azure AI Speech lands in a later ticket.
    /// </summary>
    public interface ISpeechToTextProvider
    {
        Task<SpeechToTextResult> TranscribeAsync(
            Stream audio,
            string contentType,
            CancellationToken cancellationToken = default
        );
    }
}

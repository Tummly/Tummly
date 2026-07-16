using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Fakeable provider that transcribes ephemeral guest audio.
    /// Production default is Azure AI Speech; tests and local demos use Fake.
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

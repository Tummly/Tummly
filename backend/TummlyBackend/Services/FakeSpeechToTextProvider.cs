using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Configurable fake for tests and local demos — no live Azure Speech.
    /// </summary>
    public sealed class FakeSpeechToTextProvider : ISpeechToTextProvider
    {
        private SpeechToTextResult _nextResult =
            new SpeechToTextResult.Succeeded("Fake transcript.");

        public int TranscribeCallCount { get; private set; }

        public byte[]? LastAudioBytes { get; private set; }

        public void Reset()
        {
            TranscribeCallCount = 0;
            LastAudioBytes = null;
            _nextResult = new SpeechToTextResult.Succeeded("Fake transcript.");
        }

        public void SucceedWith(string text)
        {
            _nextResult = new SpeechToTextResult.Succeeded(text);
        }

        public void EmptySpeech()
        {
            _nextResult = new SpeechToTextResult.EmptySpeech();
        }

        public void Fail()
        {
            _nextResult = new SpeechToTextResult.Failed();
        }

        public async Task<SpeechToTextResult> TranscribeAsync(
            Stream audio,
            string contentType,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var memory = new MemoryStream();
            await audio.CopyToAsync(memory, cancellationToken);
            LastAudioBytes = memory.ToArray();
            TranscribeCallCount++;

            return _nextResult;
        }
    }
}

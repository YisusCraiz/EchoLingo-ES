namespace CsTranslate.Speech;

public sealed record TranscriptSegment(string Text, TimeSpan Start, TimeSpan End, string? Language);

public interface ITranscriber : IDisposable
{
    /// <summary>
    /// Transcribes a 16kHz mono PCM16 WAV stream. Whisper.net requires this exact
    /// input format; callers are responsible for resampling before calling this.
    /// </summary>
    IAsyncEnumerable<TranscriptSegment> TranscribeAsync(Stream pcm16MonoWavStream, CancellationToken cancellationToken = default);
}

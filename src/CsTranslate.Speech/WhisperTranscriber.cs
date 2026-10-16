using Whisper.net;

namespace CsTranslate.Speech;

public sealed class WhisperTranscriber : ITranscriber
{
    private readonly WhisperFactory _factory;
    private readonly string? _language;

    /// <param name="modelPath">Path to a local GGML model file (e.g. ggml-base.bin).</param>
    /// <param name="language">
    /// A whisper language code (e.g. "ru", "en"), or null/"auto" to auto-detect the
    /// source language per the "otomatik algıla" requirement.
    /// </param>
    public WhisperTranscriber(string modelPath, string? language = null)
    {
        _factory = WhisperFactory.FromPath(modelPath);
        _language = language;
    }

    public async IAsyncEnumerable<TranscriptSegment> TranscribeAsync(
        Stream pcm16MonoWavStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var builder = _factory.CreateBuilder()
            .WithLanguage(_language ?? "auto");

        await using var processor = builder.Build();

        await foreach (SegmentData segment in processor.ProcessAsync(pcm16MonoWavStream, cancellationToken))
        {
            yield return new TranscriptSegment(segment.Text.Trim(), segment.Start, segment.End, segment.Language);
        }
    }

    public void Dispose() => _factory.Dispose();
}

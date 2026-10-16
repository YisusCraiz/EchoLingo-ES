using System.Text.RegularExpressions;
using System.Threading.Channels;
using CsTranslate.Audio;
using CsTranslate.Cleanup;
using CsTranslate.Core.Models;
using CsTranslate.Speech;
using CsTranslate.Translation;

namespace CsTranslate.Core.Pipeline;

/// <summary>
/// Wires capture -> VAD segmentation -> STT -> rule-based cleanup -> translation
/// -> overlay callback into one continuous, cancellable pipeline. Stage boundaries
/// are bounded Channels: audio capture must never block (DropOldest), downstream
/// stages can wait since they aren't time-critical to the audio device.
/// </summary>
public sealed class TranslationPipeline : IAsyncDisposable
{
    // Whisper commonly "hallucinates" a bracketed non-speech caption (background
    // music, applause, noise) instead of returning blank for non-speech audio -
    // e.g. "(bright music)", "[Music]", "♪ music playing ♪". A segment that is
    // ENTIRELY one such bracketed phrase is filtered out before translation.
    private static readonly Regex NonSpeechHallucinationPattern = new(
        @"^[\(\[♪][^\)\]]*[\)\]♪]$", RegexOptions.Compiled);

    private static bool IsNonSpeechHallucination(string text) =>
        NonSpeechHallucinationPattern.IsMatch(text.Trim());

    private readonly IAudioCaptureSource _captureSource;
    private readonly ITranscriber _transcriber;
    private readonly GlossaryCleanupService _cleanup;
    private readonly ITranslationClient _translationClient;
    private readonly AppSettings _settings;
    private readonly Action<TranslatedLine> _onTranslatedLine;
    private readonly Action<Exception>? _onError;

    private readonly VoiceActivitySegmenter _segmenter;
    private readonly Channel<AudioChunk> _audioChunks = Channel.CreateBounded<AudioChunk>(
        new BoundedChannelOptions(4) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly Channel<TranscriptSegment> _transcripts = Channel.CreateBounded<TranscriptSegment>(8);
    private readonly Channel<TranscriptSegment> _cleaned = Channel.CreateUnbounded<TranscriptSegment>();

    private CancellationTokenSource? _cts;
    private Task? _sttTask;
    private Task? _cleanupTask;
    private Task? _translationTask;

    public TranslationPipeline(
        IAudioCaptureSource captureSource,
        ITranscriber transcriber,
        GlossaryCleanupService cleanup,
        ITranslationClient translationClient,
        AppSettings settings,
        Action<TranslatedLine> onTranslatedLine,
        Action<Exception>? onError = null)
    {
        _captureSource = captureSource;
        _transcriber = transcriber;
        _cleanup = cleanup;
        _translationClient = translationClient;
        _settings = settings;
        _onTranslatedLine = onTranslatedLine;
        _onError = onError;
        _segmenter = new VoiceActivitySegmenter(settings.Vad);
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        _segmenter.ChunkReady += (_, chunk) => _audioChunks.Writer.TryWrite(chunk);
        _captureSource.DataAvailable += OnAudioDataAvailable;
        _captureSource.Start();

        _sttTask = Task.Run(() => RunSttLoopAsync(token), CancellationToken.None);
        _cleanupTask = Task.Run(() => RunCleanupLoopAsync(token), CancellationToken.None);
        _translationTask = Task.Run(() => RunTranslationLoopAsync(token), CancellationToken.None);
    }

    public async Task StopAsync()
    {
        _captureSource.DataAvailable -= OnAudioDataAvailable;
        _captureSource.Stop();
        _segmenter.Flush();

        _cts?.Cancel();
        _audioChunks.Writer.TryComplete();

        Task?[] pendingTasks = [_sttTask, _cleanupTask, _translationTask];
        Task[] tasks = pendingTasks.Where(t => t is not null).Select(t => t!).ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    private void OnAudioDataAvailable(object? sender, AudioCapturedEventArgs e)
    {
        _segmenter.Feed(e.Buffer, _captureSource.Format);
    }

    private async Task RunSttLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (AudioChunk chunk in _audioChunks.Reader.ReadAllAsync(token))
            {
                using MemoryStream wav = AudioResampler.WrapAsWav(chunk.Pcm, chunk.Format);
                using MemoryStream resampled = AudioResampler.ResampleToWhisperFormat(wav);

                await foreach (TranscriptSegment segment in _transcriber.TranscribeAsync(resampled, token))
                {
                    if (!string.IsNullOrWhiteSpace(segment.Text) && !IsNonSpeechHallucination(segment.Text))
                    {
                        await _transcripts.Writer.WriteAsync(segment, token);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            _transcripts.Writer.TryComplete();
        }
    }

    private async Task RunCleanupLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (TranscriptSegment segment in _transcripts.Reader.ReadAllAsync(token))
            {
                string cleanedText = _cleanup.Clean(segment.Text, segment.Language);
                await _cleaned.Writer.WriteAsync(segment with { Text = cleanedText }, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            _cleaned.Writer.TryComplete();
        }
    }

    private async Task RunTranslationLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (TranscriptSegment segment in _cleaned.Reader.ReadAllAsync(token))
            {
                try
                {
                    TranslationResult result = await _translationClient.TranslateAsync(
                        segment.Text,
                        _settings.TargetLanguageCode,
                        segment.Language ?? "auto",
                        token);

                    _onTranslatedLine(new TranslatedLine(
                        segment.Text,
                        result.TranslatedText,
                        result.DetectedSourceLanguage ?? segment.Language,
                        DateTimeOffset.Now));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _onError?.Invoke(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }
}

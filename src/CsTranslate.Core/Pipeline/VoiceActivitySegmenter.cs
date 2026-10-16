using CsTranslate.Audio;
using CsTranslate.Core.Models;

namespace CsTranslate.Core.Pipeline;

/// <summary>
/// Buffers raw captured PCM into ~<see cref="VadSettings.ChunkSoftSeconds"/> chunks,
/// cutting on trailing silence, with a hard cap so a continuous talker doesn't
/// starve the rest of the pipeline of output. This is the main latency lever:
/// smaller chunks reduce delay but increase per-chunk Whisper overhead.
/// </summary>
public sealed class VoiceActivitySegmenter
{
    private const double TrailingSilenceMs = 300;

    private readonly VadSettings _settings;
    private readonly List<byte> _buffer = [];
    private WaveFormatInfo? _format;
    private DateTime _bufferStartedAt;
    private DateTime _lastLoudAt = DateTime.MinValue;
    private bool _hasVoice;

    public event EventHandler<AudioChunk>? ChunkReady;

    public VoiceActivitySegmenter(VadSettings settings)
    {
        _settings = settings;
    }

    public void Feed(byte[] pcm, WaveFormatInfo format)
    {
        DateTime now = DateTime.UtcNow;
        bool hasVoice = ComputeRms(pcm, format) > _settings.SilenceThresholdRms;

        // Ignore silence until there is actual speech. Otherwise Whisper receives
        // empty chunks every few seconds and can hallucinate text, adding noise
        // and unnecessary work ahead of real translations.
        if (!_hasVoice && !hasVoice)
        {
            return;
        }

        if (_buffer.Count == 0)
        {
            _bufferStartedAt = now;
            _format = format;
        }

        _buffer.AddRange(pcm);

        if (hasVoice)
        {
            _hasVoice = true;
            _lastLoudAt = now;
        }

        double bufferedSeconds = (now - _bufferStartedAt).TotalSeconds;
        bool hardCap = bufferedSeconds >= _settings.ChunkHardCapSeconds;
        bool softCutOnTrailingSilence = bufferedSeconds >= _settings.ChunkSoftSeconds
            && (now - _lastLoudAt).TotalMilliseconds >= TrailingSilenceMs;

        if (hardCap || softCutOnTrailingSilence)
        {
            EmitChunk();
        }
    }

    /// <summary>Emits whatever's buffered. Call when capture stops so trailing audio isn't lost.</summary>
    public void Flush()
    {
        if (_buffer.Count > 0)
        {
            EmitChunk();
        }
    }

    private void EmitChunk()
    {
        if (_format is null || _buffer.Count == 0)
        {
            return;
        }

        ChunkReady?.Invoke(this, new AudioChunk { Pcm = _buffer.ToArray(), Format = _format });
        _buffer.Clear();
        _format = null;
        _bufferStartedAt = DateTime.MinValue;
        _lastLoudAt = DateTime.MinValue;
        _hasVoice = false;
    }

    private static double ComputeRms(byte[] pcm, WaveFormatInfo format)
    {
        if (format.IsFloat)
        {
            int sampleCount = pcm.Length / 4;
            if (sampleCount == 0)
            {
                return 0;
            }

            double sumSquares = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                float sample = BitConverter.ToSingle(pcm, i * 4);
                sumSquares += sample * sample;
            }

            return Math.Sqrt(sumSquares / sampleCount);
        }
        else
        {
            int sampleCount = pcm.Length / 2;
            if (sampleCount == 0)
            {
                return 0;
            }

            double sumSquares = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(pcm, i * 2);
                double normalized = sample / 32768.0;
                sumSquares += normalized * normalized;
            }

            return Math.Sqrt(sumSquares / sampleCount);
        }
    }
}

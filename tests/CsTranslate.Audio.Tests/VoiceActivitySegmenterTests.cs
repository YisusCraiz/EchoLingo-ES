using CsTranslate.Audio;
using CsTranslate.Core.Models;
using CsTranslate.Core.Pipeline;

namespace CsTranslate.Audio.Tests;

public class VoiceActivitySegmenterTests
{
    private static readonly WaveFormatInfo Format = new(16000, 1, 16, false);

    [Fact]
    public void Feed_SilenceOnly_DoesNotEmitChunks()
    {
        var segmenter = new VoiceActivitySegmenter(new VadSettings
        {
            ChunkSoftSeconds = 0.01,
            ChunkHardCapSeconds = 0.02,
        });
        int emitted = 0;
        segmenter.ChunkReady += (_, _) => emitted++;

        for (int i = 0; i < 20; i++)
        {
            segmenter.Feed(new byte[3200], Format);
        }

        segmenter.Flush();

        Assert.Equal(0, emitted);
    }

    [Fact]
    public async Task Feed_SpeechThenTrailingSilence_EmitsOneChunk()
    {
        var segmenter = new VoiceActivitySegmenter(new VadSettings
        {
            ChunkSoftSeconds = 0.01,
            ChunkHardCapSeconds = 10,
            SilenceThresholdRms = 0.01,
        });
        var emitted = new List<AudioChunk>();
        segmenter.ChunkReady += (_, chunk) => emitted.Add(chunk);

        segmenter.Feed(CreateLoudPcm(320), Format);
        await Task.Delay(350);
        segmenter.Feed(new byte[3200], Format);

        Assert.Single(emitted);
        Assert.NotEmpty(emitted[0].Pcm);
    }

    private static byte[] CreateLoudPcm(int sampleCount)
    {
        var pcm = new byte[sampleCount * sizeof(short)];
        for (int i = 0; i < sampleCount; i++)
        {
            BitConverter.TryWriteBytes(pcm.AsSpan(i * sizeof(short)), (short)12000);
        }

        return pcm;
    }
}

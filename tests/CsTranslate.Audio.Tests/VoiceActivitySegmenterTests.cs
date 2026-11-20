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
            SentencePauseMilliseconds = 300,
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

    [Fact]
    public void ShortPauseInsideSentence_KeepsSpeechTogether_ThenFlushesWithoutMorePackets()
    {
        var clock = new TestClock();
        var segmenter = new VoiceActivitySegmenter(new VadSettings(), clock);
        var chunks = new List<AudioChunk>();
        segmenter.ChunkReady += (_, chunk) => chunks.Add(chunk);
        segmenter.Feed(CreateLoudPcm(320), Format);
        clock.Advance(500);
        segmenter.CheckForSilence();
        Assert.Empty(chunks);
        segmenter.Feed(CreateLoudPcm(320), Format);
        clock.Advance(899);
        segmenter.CheckForSilence();
        Assert.Empty(chunks);
        clock.Advance(1);
        segmenter.CheckForSilence();
        Assert.Single(chunks);
        Assert.Equal(1280, chunks[0].Pcm.Length);
        segmenter.CheckForSilence();
        Assert.Single(chunks);
    }

    [Fact]
    public void ContinuousSpeech_IsNotCutAtFourSeconds_ButHasSafetyLimit()
    {
        var clock = new TestClock();
        var segmenter = new VoiceActivitySegmenter(new VadSettings(), clock);
        int emitted = 0;
        segmenter.ChunkReady += (_, _) => emitted++;
        segmenter.Feed(CreateLoudPcm(320), Format);
        for (int second = 1; second <= 11; second++)
        {
            clock.Advance(1000);
            segmenter.Feed(CreateLoudPcm(320), Format);
            Assert.Equal(0, emitted);
        }
        clock.Advance(1000);
        segmenter.Feed(CreateLoudPcm(320), Format);
        Assert.Equal(1, emitted);
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(int milliseconds) => _now += TimeSpan.FromMilliseconds(milliseconds);
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

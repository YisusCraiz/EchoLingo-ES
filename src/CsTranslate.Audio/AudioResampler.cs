using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace CsTranslate.Audio;

/// <summary>
/// Converts a captured WAV stream (whatever sample rate/channel count the capture
/// source produced) into the 16kHz mono PCM16 WAV format Whisper.net requires.
/// </summary>
public static class AudioResampler
{
    private const int WhisperSampleRate = 16000;

    /// <summary>Wraps raw captured PCM bytes (as produced by <see cref="IAudioCaptureSource.DataAvailable"/>) in a WAV container so they can be read by <see cref="WaveFileReader"/>-based code.</summary>
    public static MemoryStream WrapAsWav(byte[] pcm, WaveFormatInfo format)
    {
        var output = new MemoryStream();
        var waveFormat = format.IsFloat
            ? WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels)
            : new WaveFormat(format.SampleRate, format.BitsPerSample, format.Channels);

        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(output), waveFormat))
        {
            writer.Write(pcm, 0, pcm.Length);
        }

        output.Position = 0;
        return output;
    }

    public static MemoryStream ResampleToWhisperFormat(Stream sourceWavStream)
    {
        using var reader = new WaveFileReader(sourceWavStream);
        ISampleProvider sampleProvider = reader.ToSampleProvider();

        if (sampleProvider.WaveFormat.Channels > 1)
        {
            sampleProvider = new StereoToMonoSampleProvider(sampleProvider)
            {
                LeftVolume = 0.5f,
                RightVolume = 0.5f,
            };
        }

        if (sampleProvider.WaveFormat.SampleRate != WhisperSampleRate)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, WhisperSampleRate);
        }

        var output = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(output), sampleProvider.ToWaveFormat16Bit()))
        {
            var buffer = new float[4096];
            int samplesRead;
            while ((samplesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.WriteSamples(buffer, 0, samplesRead);
            }
        }

        output.Position = 0;
        return output;
    }

    private static WaveFormat ToWaveFormat16Bit(this ISampleProvider sampleProvider) =>
        new(sampleProvider.WaveFormat.SampleRate, 16, sampleProvider.WaveFormat.Channels);
}

namespace CsTranslate.Audio;

public sealed class AudioCapturedEventArgs : EventArgs
{
    public required byte[] Buffer { get; init; }
    public required int BytesRecorded { get; init; }
}

public sealed record WaveFormatInfo(int SampleRate, int Channels, int BitsPerSample, bool IsFloat);

public interface IAudioCaptureSource : IDisposable
{
    WaveFormatInfo Format { get; }

    event EventHandler<AudioCapturedEventArgs>? DataAvailable;

    void Start();

    void Stop();
}

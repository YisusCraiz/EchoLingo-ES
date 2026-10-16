using NAudio.Wave;

namespace CsTranslate.Audio;

/// <summary>
/// Fallback capture strategy: records the default output device's entire mix,
/// not a single process. Used when <see cref="ProcessLoopbackCapture"/> is
/// unavailable/unreliable; the documented workaround is routing the target
/// app's output to a virtual audio cable (e.g. VB-Cable) set as the default
/// device, so this loopback only picks up that app's audio in practice.
/// </summary>
public sealed class WasapiSystemLoopbackCapture : IAudioCaptureSource
{
    private readonly WasapiLoopbackCapture _capture = new();

    public WaveFormatInfo Format { get; }

    public event EventHandler<AudioCapturedEventArgs>? DataAvailable;

    public WasapiSystemLoopbackCapture()
    {
        var format = _capture.WaveFormat;
        Format = new WaveFormatInfo(format.SampleRate, format.Channels, format.BitsPerSample, IsFloat: format.Encoding == WaveFormatEncoding.IeeeFloat);
        _capture.DataAvailable += OnDataAvailable;
    }

    private void OnDataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0)
        {
            return;
        }

        var buffer = new byte[e.BytesRecorded];
        Array.Copy(e.Buffer, buffer, e.BytesRecorded);
        DataAvailable?.Invoke(this, new AudioCapturedEventArgs { Buffer = buffer, BytesRecorded = e.BytesRecorded });
    }

    public void Start() => _capture.StartRecording();

    public void Stop() => _capture.StopRecording();

    public void Dispose()
    {
        _capture.DataAvailable -= OnDataAvailable;
        _capture.Dispose();
    }
}

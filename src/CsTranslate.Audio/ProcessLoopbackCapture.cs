using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CsTranslate.Audio.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace CsTranslate.Audio;

/// <summary>
/// Captures only the audio rendered by one process (and optionally its child
/// processes), using the Windows 10 20H1+ WASAPI "process loopback" activation.
/// This is the primary capture strategy; <see cref="WasapiSystemLoopbackCapture"/>
/// is the documented fallback when this path is unavailable/unreliable.
/// </summary>
[SupportedOSPlatform("windows10.0.19041")]
public sealed class ProcessLoopbackCapture : IAudioCaptureSource
{
    // Matches Microsoft's official ApplicationLoopback sample exactly (16-bit PCM,
    // 44.1kHz, stereo) rather than the theoretically-nicer float32/48kHz: without
    // AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM, requesting a format that doesn't match
    // what the loopback engine actually produces internally yields garbage samples,
    // not an error - this proven-working combination avoids that trap entirely.
    private const int SampleRate = 44100;
    private const int Channels = 2;
    private const int BitsPerSample = 16;
    private const uint WaveFormatPcm = 1;
    private const uint AudclntBufferflagsSilent = 0x1;
    private const uint WaitObject0 = 0;

    private readonly int _processId;
    private readonly bool _includeChildProcesses;

    private IAudioClient? _audioClient;
    private IAudioCaptureClient? _captureClient;
    private SafeFileHandle? _eventHandle;
    private Thread? _captureThread;
    private volatile bool _stopping;

    public WaveFormatInfo Format { get; } = new WaveFormatInfo(SampleRate, Channels, BitsPerSample, IsFloat: false);

    public event EventHandler<AudioCapturedEventArgs>? DataAvailable;

    public ProcessLoopbackCapture(int processId, bool includeChildProcesses = true)
    {
        _processId = processId;
        _includeChildProcesses = includeChildProcesses;
    }

    public void Start()
    {
        if (_audioClient is not null)
        {
            throw new InvalidOperationException("Capture already started.");
        }

        // All WASAPI/COM calls for this object's lifetime - activation, Initialize,
        // and the capture-drain loop - must happen on the SAME dedicated (MTA)
        // thread. Doing the activation on a caller thread that turns out to be STA
        // (e.g. a WPF UI thread) produces an RCW that fails QueryInterface for
        // IAudioClient with E_NOINTERFACE later - observed directly during
        // development. A plain `new Thread(...)` defaults to MTA regardless of the
        // caller's apartment, so running everything there sidesteps the issue.
        _stopping = false;
        Exception? startupException = null;
        using var setupComplete = new ManualResetEventSlim(false);

        _captureThread = new Thread(() => CaptureThreadMain(setupComplete, ex => startupException = ex))
        {
            IsBackground = true,
            Name = "ProcessLoopbackCapture",
        };
        _captureThread.Start();

        setupComplete.Wait();
        if (startupException is not null)
        {
            _captureThread = null;
            _audioClient = null;
            throw startupException;
        }
    }

    private unsafe void CaptureThreadMain(ManualResetEventSlim setupComplete, Action<Exception> onStartupFailed)
    {
        try
        {
            using var activator = new ProcessLoopbackActivator();
            _audioClient = activator.ActivateForProcess((uint)_processId, _includeChildProcesses);

            var format = default(WAVEFORMATEX);
            format.wFormatTag = (ushort)WaveFormatPcm;
            format.nChannels = Channels;
            format.nSamplesPerSec = SampleRate;
            format.wBitsPerSample = BitsPerSample;
            format.cbSize = 0;
            format.nBlockAlign = (ushort)(format.nChannels * (format.wBitsPerSample / 8));
            format.nAvgBytesPerSec = format.nSamplesPerSec * format.nBlockAlign;

            // AUDCLNT_STREAMFLAGS_LOOPBACK is required even for process-loopback
            // activation - the resulting IAudioClient is still consumed via
            // IAudioCaptureClient exactly like a normal device-loopback capture.
            // AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM tells the engine to convert its
            // internal float mix format to the PCM format requested here.
            _audioClient.Initialize(
                AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                PInvoke.AUDCLNT_STREAMFLAGS_LOOPBACK | PInvoke.AUDCLNT_STREAMFLAGS_EVENTCALLBACK | PInvoke.AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM,
                hnsBufferDuration: 0,
                hnsPeriodicity: 0,
                &format,
                null);

            _eventHandle = PInvoke.CreateEvent(null, bManualReset: false, bInitialState: false, lpName: (string?)null);
            if (_eventHandle.IsInvalid)
            {
                throw new InvalidOperationException("Failed to create the WASAPI capture event handle.");
            }

            _audioClient.SetEventHandle(new HANDLE(_eventHandle.DangerousGetHandle()));

            Guid captureClientGuid = typeof(IAudioCaptureClient).GUID;
            _audioClient.GetService(&captureClientGuid, out object captureObj);
            _captureClient = (IAudioCaptureClient)captureObj;

            _audioClient.Start();
        }
        catch (Exception ex)
        {
            onStartupFailed(ex);
            setupComplete.Set();
            return;
        }

        setupComplete.Set();

        while (!_stopping)
        {
            WAIT_EVENT waitResult = PInvoke.WaitForSingleObject(_eventHandle!, 200);
            if (_stopping)
            {
                break;
            }

            if ((uint)waitResult != WaitObject0)
            {
                continue;
            }

            DrainAvailablePackets();
        }

        try
        {
            _audioClient.Stop();
        }
        catch (COMException)
        {
            // Best-effort: the device may already be gone (target process exited).
        }
    }

    private unsafe void DrainAvailablePackets()
    {
        if (_captureClient is null)
        {
            return;
        }

        _captureClient.GetNextPacketSize(out uint packetLength);
        while (packetLength != 0)
        {
            byte* data;
            uint framesAvailable;
            uint flags;
            _captureClient.GetBuffer(&data, out framesAvailable, out flags, null, null);

            int bytesRecorded = (int)(framesAvailable * Format.Channels * (Format.BitsPerSample / 8));
            if (bytesRecorded > 0)
            {
                var buffer = new byte[bytesRecorded];
                if ((flags & AudclntBufferflagsSilent) == 0)
                {
                    Marshal.Copy((IntPtr)data, buffer, 0, bytesRecorded);
                }

                DataAvailable?.Invoke(this, new AudioCapturedEventArgs { Buffer = buffer, BytesRecorded = bytesRecorded });
            }

            _captureClient.ReleaseBuffer(framesAvailable);
            _captureClient.GetNextPacketSize(out packetLength);
        }
    }

    public void Stop()
    {
        if (_audioClient is null)
        {
            return;
        }

        // IAudioClient.Stop() is called on the capture thread itself (see
        // CaptureThreadMain) rather than here, to keep every COM call on the one
        // apartment that activated the interface.
        _stopping = true;
        _captureThread?.Join(TimeSpan.FromSeconds(1));
        _captureThread = null;
    }

    public void Dispose()
    {
        Stop();

        if (_captureClient is not null)
        {
            Marshal.ReleaseComObject(_captureClient);
            _captureClient = null;
        }

        if (_audioClient is not null)
        {
            Marshal.ReleaseComObject(_audioClient);
            _audioClient = null;
        }

        _eventHandle?.Dispose();
        _eventHandle = null;
    }
}

using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;

namespace CsTranslate.Audio.Interop;

/// <summary>
/// Synchronous wrapper around ActivateAudioInterfaceAsync for the Windows 10 20H1+
/// "process loopback" virtual audio device. This is the highest-risk piece of the
/// whole app: there is no NuGet package for it, only Microsoft's C++ sample.
/// </summary>
[SupportedOSPlatform("windows10.0.19041")]
internal sealed class ProcessLoopbackActivator : IActivateAudioInterfaceCompletionHandler, IDisposable
{
    // The documented "virtual audio device" path used to request process-loopback
    // activation instead of a real endpoint device. See Microsoft's
    // ApplicationLoopback sample (VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK).
    private const string VirtualAudioDeviceProcessLoopback = @"VAD\Process_Loopback";

    private readonly ManualResetEventSlim _completed = new(initialState: false);
    private HRESULT _activateResult;
    private object? _activatedInterface;

    public unsafe IAudioClient ActivateForProcess(uint processId, bool includeChildProcesses)
    {
        var activationParams = default(AUDIOCLIENT_ACTIVATION_PARAMS);
        activationParams.ActivationType = AUDIOCLIENT_ACTIVATION_TYPE.AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK;
        activationParams.ProcessLoopbackParams.TargetProcessId = processId;
        activationParams.ProcessLoopbackParams.ProcessLoopbackMode = includeChildProcesses
            ? PROCESS_LOOPBACK_MODE.PROCESS_LOOPBACK_MODE_INCLUDE_TARGET_PROCESS_TREE
            : PROCESS_LOOPBACK_MODE.PROCESS_LOOPBACK_MODE_EXCLUDE_TARGET_PROCESS_TREE;

        var propvariant = default(PROPVARIANT);
        propvariant.vt = VARENUM.VT_BLOB;
        propvariant.blob.cbSize = (uint)sizeof(AUDIOCLIENT_ACTIVATION_PARAMS);
        propvariant.blob.pBlobData = (byte*)&activationParams;

        Guid riid = typeof(IAudioClient).GUID;
        HRESULT hr = PInvoke.ActivateAudioInterfaceAsync(
            VirtualAudioDeviceProcessLoopback,
            riid,
            propvariant,
            this,
            out IActivateAudioInterfaceAsyncOperation _);
        hr.ThrowOnFailure();

        // Block until Windows calls ActivateCompleted below. This keeps
        // `activationParams` alive on the stack for the native call's duration,
        // which is required since PROPVARIANT.blob only carries a raw pointer to it.
        _completed.Wait();

        _activateResult.ThrowOnFailure();
        return (IAudioClient)_activatedInterface!;
    }

    unsafe void IActivateAudioInterfaceCompletionHandler.ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
    {
        HRESULT result;
        activateOperation.GetActivateResult(&result, out object activatedInterface);
        _activateResult = result;
        _activatedInterface = activatedInterface;
        _completed.Set();
    }

    public void Dispose() => _completed.Dispose();
}

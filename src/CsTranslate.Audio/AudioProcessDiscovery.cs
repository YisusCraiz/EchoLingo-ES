using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace CsTranslate.Audio;

/// <summary>
/// An application process that currently owns an active Windows render-audio session.
/// </summary>
public sealed record AudioProcessInfo(int ProcessId, string ProcessName, string DisplayName)
{
    public string Label => string.IsNullOrWhiteSpace(DisplayName) || DisplayName == ProcessName
        ? $"{ProcessName}.exe · PID {ProcessId}"
        : $"{DisplayName} · {ProcessName}.exe · PID {ProcessId}";
}

/// <summary>
/// Finds applications that are actively playing sound on available Windows output devices.
/// The returned process ID is the audio session owner and can be passed directly to
/// <see cref="ProcessLoopbackCapture"/>.
/// </summary>
public static class AudioProcessDiscovery
{
    public static IReadOnlyList<AudioProcessInfo> GetActiveAudioProcesses()
    {
        var processes = new Dictionary<int, AudioProcessInfo>();
        using var enumerator = new MMDeviceEnumerator();

        foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            using (device)
            {
                AudioSessionCollection sessions;
                try
                {
                    sessions = device.AudioSessionManager.Sessions;
                }
                catch
                {
                    // An output device can disappear while Windows is enumerating endpoints.
                    continue;
                }

                for (int i = 0; i < sessions.Count; i++)
                {
                    AudioSessionControl? session = null;
                    try
                    {
                        session = sessions[i];
                        if (session.State != AudioSessionState.AudioSessionStateActive)
                        {
                            continue;
                        }

                        uint processId = session.GetProcessID;
                        if (processId == 0 || processId == Environment.ProcessId)
                        {
                            continue;
                        }

                        using Process process = Process.GetProcessById((int)processId);
                        string processName = process.ProcessName;
                        string displayName = session.DisplayName;
                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = process.MainWindowTitle;
                        }

                        processes[(int)processId] = new AudioProcessInfo(
                            (int)processId,
                            processName,
                            string.IsNullOrWhiteSpace(displayName) ? processName : displayName);
                    }
                    catch
                    {
                        // Audio sessions and their processes can end between enumeration and inspection.
                    }
                    finally
                    {
                        session?.Dispose();
                    }
                }
            }
        }

        return processes.Values
            .OrderBy(process => process.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(process => process.ProcessName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}

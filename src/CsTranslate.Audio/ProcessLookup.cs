using System.Diagnostics;

namespace CsTranslate.Audio;

public static class ProcessLookup
{
    /// <summary>
    /// Finds the first running process whose executable name matches (case-insensitive,
    /// with or without the ".exe" suffix). Used to resolve a user-facing process name
    /// (e.g. "Discord.exe") to the process ID that <see cref="ProcessLoopbackCapture"/> needs.
    /// </summary>
    public static int? FindProcessId(string processName)
    {
        if (int.TryParse(processName, out int explicitPid))
        {
            return explicitPid;
        }

        string normalized = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName[..^4]
            : processName;

        Process[] matches = Process.GetProcessesByName(normalized);
        try
        {
            return matches.Length > 0 ? matches[0].Id : null;
        }
        finally
        {
            foreach (Process process in matches)
            {
                process.Dispose();
            }
        }
    }
}

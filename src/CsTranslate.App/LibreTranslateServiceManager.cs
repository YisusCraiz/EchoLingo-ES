using System.Diagnostics;
using System.Net;
using CsTranslate.Translation;

namespace CsTranslate.App;

/// <summary>
/// Keeps LibreTranslate and its Python packages isolated from the user's
/// system-wide Python installation. Python 3.13 is required for a new setup;
/// the managed virtual environment and Argos language models stay in LocalAppData.
/// </summary>
internal sealed class LibreTranslateServiceManager : IDisposable
{
    private const string LibreTranslateVersion = "1.9.6";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(10);
    private readonly string _runtimeDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EchoLingo ES", "LibreTranslate");
    private Process? _server;
    private readonly object _logLock = new();

    public async Task EnsureStartedAsync(string baseUrl, Action<string> reportStatus, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttp ||
            !uri.IsLoopback)
        {
            throw new InvalidOperationException(
                "La preparación automática solo funciona con una dirección local HTTP, por ejemplo http://localhost:5000. Para una dirección remota, configura ese servidor por separado.");
        }

        if (await new LibreTranslateClient(baseUrl).IsHealthyAsync(cancellationToken))
        {
            return;
        }

        Directory.CreateDirectory(_runtimeDirectory);
        string venvPython = Path.Combine(_runtimeDirectory, "venv", "Scripts", "python.exe");
        string serverExe = Path.Combine(_runtimeDirectory, "venv", "Scripts", "libretranslate.exe");

        if (!File.Exists(venvPython))
        {
            reportStatus("Buscando Python 3.13...");
            await RunLauncherAsync(["-3.13", "--version"], cancellationToken);
            reportStatus("Creando un entorno aislado...");
            await RunLauncherAsync(["-3.13", "-m", "venv", Path.Combine(_runtimeDirectory, "venv")], cancellationToken);
        }

        string versionFile = Path.Combine(_runtimeDirectory, "libretranslate.version");
        bool translatorNeedsInstall = !File.Exists(serverExe) ||
            !File.Exists(versionFile) ||
            !string.Equals(File.ReadAllText(versionFile).Trim(), LibreTranslateVersion, StringComparison.Ordinal);
        if (translatorNeedsInstall)
        {
            reportStatus("Descargando e instalando LibreTranslate. La primera descarga puede tardar...");

            await RunPythonAsync(venvPython, ["-m", "pip", "install", "--upgrade", "--disable-pip-version-check", "setuptools<81", $"libretranslate=={LibreTranslateVersion}"], cancellationToken);
            File.WriteAllText(versionFile, LibreTranslateVersion);
        }

        if (_server is { HasExited: false })
        {
            _server.Kill(entireProcessTree: true);
            _server.Dispose();
        }

        string logPath = Path.Combine(_runtimeDirectory, "server.log");
        var startInfo = new ProcessStartInfo
        {
            FileName = serverExe,
            WorkingDirectory = _runtimeDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--load-only");
        startInfo.ArgumentList.Add("en,es");
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add("127.0.0.1");
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(uri.Port.ToString());
        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["ARGOS_DEVICE_TYPE"] = "cpu";

        _server = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _server.OutputDataReceived += (_, e) => AppendLog(logPath, e.Data);
        _server.ErrorDataReceived += (_, e) => AppendLog(logPath, e.Data);
        if (!_server.Start())
        {
            throw new InvalidOperationException("Windows no pudo iniciar LibreTranslate.");
        }

        _server.BeginOutputReadLine();
        _server.BeginErrorReadLine();

        reportStatus("Iniciando el traductor y preparando inglés/español...");
        DateTimeOffset deadline = DateTimeOffset.UtcNow + StartupTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_server.HasExited)
            {
                throw new InvalidOperationException(
                    $"LibreTranslate se cerró durante el inicio. Revisa el registro: {logPath}");
            }

            if (await new LibreTranslateClient(baseUrl).IsHealthyAsync(cancellationToken))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException($"LibreTranslate no respondió después de 10 minutos. Revisa el registro: {logPath}");
    }

    private static Task RunLauncherAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        string localLauncher = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Python", "Launcher", "py.exe");
        string appExecutionAlias = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "py.exe");
        string executable = File.Exists(localLauncher)
            ? localLauncher
            : File.Exists(appExecutionAlias)
                ? appExecutionAlias
                : "py.exe";
        return RunProcessAsync(executable, arguments, null, cancellationToken);
    }

    private static Task RunPythonAsync(string pythonPath, IReadOnlyList<string> arguments, CancellationToken cancellationToken) =>
        RunProcessAsync(pythonPath, arguments, null, cancellationToken);

    private static async Task RunProcessAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

        using var process = new Process { StartInfo = startInfo };
        var output = new System.Text.StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"No se pudo iniciar {executable}.");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException(
                "No se encontró Python 3.13. Vuelve a ejecutar el instalador de EchoLingo y marca “Instalar Python 3.13”, o instálalo desde python.org y vuelve a intentar.", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            string details = output.ToString();
            if (details.Length > 3000) details = details[^3000..];
            throw new InvalidOperationException(
                $"{Path.GetFileName(executable)} terminó con código {process.ExitCode}." +
                (string.IsNullOrWhiteSpace(details) ? string.Empty : Environment.NewLine + details));
        }
    }

    private void AppendLog(string path, string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        lock (_logLock)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }

    public void Dispose()
    {
        if (_server is { HasExited: false })
        {
            try { _server.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        }

        _server?.Dispose();
        _server = null;
    }
}

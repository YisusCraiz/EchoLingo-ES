using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace CsTranslate.App;

internal static class UpdateChecker
{
    private const string ApiUrl = "https://api.github.com/repos/YisusCraiz/EchoLingo-ES/releases/latest";
    private const string ReleasesUrl = "https://github.com/YisusCraiz/EchoLingo-ES/releases/latest";
    private static readonly HttpClient Client = CreateClient();

    public static async Task CheckAndPromptAsync()
    {
        try
        {
            using var response = await Client.GetAsync(ApiUrl);
            if (!response.IsSuccessStatusCode)
                return;
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            if (!root.TryGetProperty("tag_name", out var tagElement))
                return;
            string tag = tagElement.GetString() ?? string.Empty;
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var latestVersion))
                return;
            var rawVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);
            var currentVersion = new Version(rawVersion.Major, rawVersion.Minor, Math.Max(0, rawVersion.Build));
            if (latestVersion <= currentVersion || WasSkipped(tag))
                return;

            var answer = MessageBox.Show(
                "Hay una nueva versión de EchoLingo ES (" + tag + ").\n\n" +
                "Sí: abrir la página para descargarla.\nNo: saltar esta versión.",
                "Actualización disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (answer == MessageBoxResult.No)
            {
                SaveSkippedVersion(tag);
                return;
            }
            Process.Start(new ProcessStartInfo(ReleasesUrl) { UseShellExecute = true });
        }
        catch
        {
            // Si GitHub no responde, la aplicación continúa normalmente.
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EchoLingo-ES-Updater/0.1");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static string SkippedVersionPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EchoLingo-ES", "skipped-version.txt");

    private static bool WasSkipped(string version)
    {
        try
        {
            return File.Exists(SkippedVersionPath) &&
                string.Equals(File.ReadAllText(SkippedVersionPath).Trim(), version, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static void SaveSkippedVersion(string version)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SkippedVersionPath)!);
            File.WriteAllText(SkippedVersionPath, version);
        }
        catch { }
    }
}

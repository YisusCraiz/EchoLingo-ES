using System.IO;
using System.Windows;
using System.Windows.Threading;
using CsTranslate.App.ViewModels;
using CsTranslate.App.Views;

namespace CsTranslate.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        try
        {
            var overlayViewModel = new OverlayViewModel();
            var overlay = new OverlayWindow(overlayViewModel);
            overlay.Show();
            var settingsWindow = new SettingsWindow(overlayViewModel);
            settingsWindow.Show();
            _ = UpdateChecker.CheckAndPromptAsync();
        }
        catch (Exception ex)
        {
            TryLogError("startup", ex);
            throw;
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        TryLogError("runtime", e.Exception);
        MessageBox.Show(
            "Ocurrió un error inesperado:\n\n" + e.Exception.Message + "\n\nConsulta el registro de errores para más detalles.",
            "EchoLingo ES - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void TryLogError(string context, Exception ex)
    {
        try
        {
            string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EchoLingo-ES");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(Path.Combine(logDir, "error.log"),
                "[" + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] (" + context + ") " + ex + "\n\n");
        }
        catch { }
    }
}

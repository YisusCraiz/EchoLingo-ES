using System.ComponentModel;
using System.IO;
using System.Windows;
using CsTranslate.App.ViewModels;
using CsTranslate.Audio;
using CsTranslate.Cleanup;
using CsTranslate.Core.Models;
using CsTranslate.Core.Pipeline;
using CsTranslate.Core.Settings;
using CsTranslate.Speech;
using CsTranslate.Translation;

namespace CsTranslate.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly OverlayViewModel _overlayViewModel;
    private readonly LibreTranslateServiceManager _translationService = new();

    private AppSettings _settings;
    private IAudioCaptureSource? _captureSource;
    private ITranscriber? _transcriber;
    private TranslationPipeline? _pipeline;
    private bool _shuttingDown;

    public SettingsWindow(OverlayViewModel overlayViewModel)
    {
        _overlayViewModel = overlayViewModel;
        _settings = _settingsService.Load();
        InitializeComponent();

        ProcessNameBox.Text = _settings.TargetProcessName;
        TargetLanguageBox.Text = _settings.TargetLanguageCode;
        WhisperModelPathBox.Text = _settings.WhisperModelPath;
        WhisperLanguageBox.Text = _settings.WhisperLanguage ?? string.Empty;
        LibreTranslateUrlBox.Text = _settings.LibreTranslateBaseUrl;
    }

    private async void OnStartClicked(object sender, RoutedEventArgs e)
    {
        _settings.TargetProcessName = ProcessNameBox.Text.Trim();
        _settings.TargetLanguageCode = TargetLanguageBox.Text.Trim();
        _settings.WhisperModelPath = WhisperModelPathBox.Text.Trim();
        _settings.WhisperLanguage = string.IsNullOrWhiteSpace(WhisperLanguageBox.Text) ? null : WhisperLanguageBox.Text.Trim();
        _settings.LibreTranslateBaseUrl = LibreTranslateUrlBox.Text.Trim();
        _settingsService.Save(_settings);

        int? processId = ProcessLookup.FindProcessId(_settings.TargetProcessName);
        if (processId is null)
        {
            StatusText.Text = $"No se encontró una aplicación en ejecución llamada '{_settings.TargetProcessName}'.";
            return;
        }

        if (!File.Exists(_settings.WhisperModelPath))
        {
            StatusText.Text = $"No se encontró el archivo del modelo Whisper: {_settings.WhisperModelPath}";
            return;
        }

        var translationClient = new LibreTranslateClient(_settings.LibreTranslateBaseUrl);
        if (!await translationClient.IsHealthyAsync())
        {
            MessageBoxResult choice = MessageBox.Show(
                "EchoLingo puede preparar LibreTranslate en un entorno aislado dentro de tu usuario. Se necesita Python 3.12 y conexión a internet. La primera descarga de paquetes y modelos puede tardar varios minutos y ocupar bastante espacio. ¿Continuar?",
                "Preparar traducción",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (choice != MessageBoxResult.Yes)
            {
                StatusText.Text = "Preparación cancelada.";
                return;
            }

            StartButton.IsEnabled = false;
            StatusText.Text = "Preparando el traductor...";
            try
            {
                await _translationService.EnsureStartedAsync(
                    _settings.LibreTranslateBaseUrl,
                    status => StatusText.Text = status);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"No se pudo preparar LibreTranslate: {ex.Message}";
                StartButton.IsEnabled = true;
                return;
            }
        }

        StartButton.IsEnabled = false;
        StatusText.Text = "Iniciando...";

        try
        {
            _captureSource = new ProcessLoopbackCapture(processId.Value);
            _transcriber = new WhisperTranscriber(_settings.WhisperModelPath, _settings.WhisperLanguage);
            var cleanup = new GlossaryCleanupService(Path.Combine(AppContext.BaseDirectory, "Rules"));

            _pipeline = new TranslationPipeline(
                _captureSource,
                _transcriber,
                cleanup,
                translationClient,
                _settings,
                onTranslatedLine: line => Dispatcher.Invoke(() => _overlayViewModel.AddLine(line)),
                onError: ex => Dispatcher.Invoke(() => StatusText.Text = $"Error: {ex.Message}"));

            _pipeline.Start();

            StatusText.Text = $"Traduciendo: {_settings.TargetProcessName} (PID {processId}) -> {_settings.TargetLanguageCode}";
            StopButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo iniciar: {ex.Message}";

            _pipeline = null;
            _captureSource?.Dispose();
            _captureSource = null;
            _transcriber?.Dispose();
            _transcriber = null;

            StartButton.IsEnabled = true;
        }
    }

    private async void OnStopClicked(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        StatusText.Text = "Deteniendo...";

        await StopPipelineIfRunningAsync();

        StatusText.Text = "Detenido.";
        StartButton.IsEnabled = true;
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_shuttingDown)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        _shuttingDown = true;

        StartButton.IsEnabled = false;
        StopButton.IsEnabled = false;
        CloseButton.IsEnabled = false;
        StatusText.Text = "Cerrando...";

        await StopPipelineIfRunningAsync();
        _translationService.Dispose();

        Application.Current.Shutdown();
    }

    private async Task StopPipelineIfRunningAsync()
    {
        if (_pipeline is not null)
        {
            await _pipeline.StopAsync();
            _pipeline = null;
        }

        _captureSource?.Dispose();
        _captureSource = null;
        _transcriber?.Dispose();
        _transcriber = null;
    }
}

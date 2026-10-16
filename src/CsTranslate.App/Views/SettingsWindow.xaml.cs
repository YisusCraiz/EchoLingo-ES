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
            StatusText.Text = $"'{_settings.TargetProcessName}' no se encontró una aplicación en ejecución llamada.";
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
            StatusText.Text = $"LibreTranslate {_settings.LibreTranslateBaseUrl} no está disponible en esa dirección.";
            return;
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
                onError: ex => Dispatcher.Invoke(() => StatusText.Text = $"Hata: {ex.Message}"));

            _pipeline.Start();

            StatusText.Text =  $"Traduciendo: {_settings.TargetProcessName} (PID {processId}) -> {_settings.TargetLanguageCode}";
            StopButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text =  $"No se pudo iniciar: {ex.Message}";

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

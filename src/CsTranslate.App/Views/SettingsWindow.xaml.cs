using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
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
    private readonly SettingsService _settingsService;
    private readonly OverlayViewModel _overlayViewModel;
    private readonly LibreTranslateServiceManager _translationService = new();

    private readonly AppSettings _settings;
    private IAudioCaptureSource? _captureSource;
    private ITranscriber? _transcriber;
    private TranslationPipeline? _pipeline;
    private bool _shuttingDown;
    private bool _starting;
    private double _audioPeak;
    private long _lastAudioTicks;
    private long _captureStartedTicks;
    private readonly DispatcherTimer _audioMeterTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private AudioProcessInfo? _selectedAudioProcess;

    public SettingsWindow(OverlayViewModel overlayViewModel, AppSettings settings, SettingsService settingsService)
    {
        _overlayViewModel = overlayViewModel;
        _settings = settings;
        _settingsService = settingsService;
        InitializeComponent();

        ProcessNameBox.Text = _settings.TargetProcessName;
        TargetLanguageBox.Text = _settings.TargetLanguageCode;
        WhisperModelPathBox.Text = _settings.WhisperModelPath;
        WhisperLanguageBox.Text = _settings.WhisperLanguage ?? string.Empty;
        LibreTranslateUrlBox.Text = _settings.LibreTranslateBaseUrl;
        TargetLanguagePicker.SelectedValue = _settings.TargetLanguageCode.Trim().ToLowerInvariant();
        SpokenLanguagePicker.SelectedValue = _settings.WhisperLanguage?.Trim().ToLowerInvariant() ?? string.Empty;
        Height = Math.Min(Height, Math.Max(MinHeight, SystemParameters.WorkArea.Height - 24));
        Loaded += async (_, _) => await RefreshAudioProcessesAsync();
        _audioMeterTimer.Tick += (_, _) => UpdateAudioMeter();
        _audioMeterTimer.Start();
        Closed += (_, _) => _audioMeterTimer.Stop();
    }

    private void OnTargetLanguageChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TargetLanguagePicker.SelectedValue is string code && TargetLanguageBox is not null)
        {
            TargetLanguageBox.Text = code;
        }
    }

    private void OnSpokenLanguageChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SpokenLanguagePicker.SelectedValue is string code && WhisperLanguageBox is not null)
        {
            WhisperLanguageBox.Text = code;
        }
    }

    private async void OnDetectAudioClicked(object sender, RoutedEventArgs e)
    {
        await RefreshAudioProcessesAsync();
    }

    private async Task RefreshAudioProcessesAsync()
    {
        int? previouslySelectedProcessId = _selectedAudioProcess?.ProcessId;
        DetectAudioButton.IsEnabled = false;
        StatusText.Text = "Buscando aplicaciones que estén reproduciendo audio...";

        try
        {
            AudioProcessInfo[] activeProcesses = await Task.Run(
                () => AudioProcessDiscovery.GetActiveAudioProcesses().ToArray());

            ActiveAudioProcessesComboBox.ItemsSource = activeProcesses;
            AudioProcessInfo? selection = previouslySelectedProcessId is int previousId
                ? activeProcesses.FirstOrDefault(process => process.ProcessId == previousId)
                : null;

            if (selection is null && activeProcesses.Length == 1)
            {
                selection = activeProcesses[0];
            }

            ActiveAudioProcessesComboBox.SelectedItem = selection;
            _selectedAudioProcess = selection;

            if (activeProcesses.Length == 0)
            {
                StatusText.Text = "No detecté audio activo. Reproduce una voz o sonido y pulsa «Buscar audio».";
            }
            else if (selection is not null)
            {
                StatusText.Text = $"Fuente detectada: {selection.DisplayName}. Pulsa Iniciar para capturarla.";
            }
            else
            {
                StatusText.Text = $"Detecté {activeProcesses.Length} aplicaciones con audio. Selecciona la que quieres capturar.";
            }
        }
        catch (Exception ex)
        {
            _selectedAudioProcess = null;
            ActiveAudioProcessesComboBox.ItemsSource = Array.Empty<AudioProcessInfo>();
            StatusText.Text = $"No se pudieron detectar las aplicaciones de audio: {ex.Message}";
        }
        finally
        {
            DetectAudioButton.IsEnabled = true;
        }
    }

    private void OnAudioProcessSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ActiveAudioProcessesComboBox.SelectedItem is not AudioProcessInfo selected)
        {
            return;
        }

        _selectedAudioProcess = selected;
        ProcessNameBox.Text = selected.ProcessName;
        StatusText.Text = _pipeline is null
            ? $"Fuente seleccionada: {selected.DisplayName}. Pulsa Iniciar subtítulos."
            : $"Fuente seleccionada: {selected.DisplayName}. Pulsa Detener y después Iniciar para cambiar.";
    }

    private void OnBrowseModelClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecciona el modelo de Whisper",
            Filter = "Modelos Whisper (*.bin)|*.bin|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            FileName = Path.GetFileName(WhisperModelPathBox.Text)
        };

        if (dialog.ShowDialog(this) == true)
        {
            WhisperModelPathBox.Text = dialog.FileName;
        }
    }

    private void OnPreviewOverlayClicked(object sender, RoutedEventArgs e)
    {
        Application.Current.Windows.OfType<OverlayWindow>().FirstOrDefault()?.ShowPreview();
    }

    private void OnCaptureAudio(object? sender, AudioCapturedEventArgs e)
    {
        // ProcessLoopbackCapture delivers signed 16-bit PCM. Do not block its capture thread.
        int count = Math.Min(e.BytesRecorded, e.Buffer.Length);
        double peak = 0;
        for (int offset = 0; offset + 1 < count; offset += 2)
        {
            peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(e.Buffer, offset)) / 32768.0);
        }
        double previous = Volatile.Read(ref _audioPeak);
        Volatile.Write(ref _audioPeak, Math.Max(previous, peak));
        if (peak > 0.001)
        {
            Interlocked.Exchange(ref _lastAudioTicks, DateTime.UtcNow.Ticks);
        }
    }

    private void UpdateAudioMeter()
    {
        double peak = Interlocked.Exchange(ref _audioPeak, 0);
        AudioLevelMeter.Value = Math.Clamp(peak * 100, 0, 100);
        if (_pipeline is null)
        {
            AudioLevelText.Text = "Inicia los subtítulos para medir el audio.";
            return;
        }
        long last = Interlocked.Read(ref _lastAudioTicks);
        double seconds = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - Math.Max(last, _captureStartedTicks)).TotalSeconds;
        AudioLevelText.Text = seconds < 2 && last != 0
            ? "Se está recibiendo audio. El nivel indica sonido, no necesariamente voz."
            : seconds >= 5
                ? "Sin señal reciente: reproduce una voz y comprueba que elegiste la aplicación correcta."
                : "Captura iniciada. Esperando audio de la aplicación...";
    }

    private async void OnStartClicked(object sender, RoutedEventArgs e)
    {
        if (_starting || _pipeline is not null || _shuttingDown)
        {
            return;
        }
        _starting = true;
        StartButton.IsEnabled = false;
        try
        {
            await StartTranslationAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo iniciar: {ex.Message}";
        }
        finally
        {
            _starting = false;
            StartButton.IsEnabled = _pipeline is null && !_shuttingDown;
        }
    }

    private async Task StartTranslationAsync()
    {
        _settings.TargetProcessName = ProcessNameBox.Text.Trim();
        _settings.TargetLanguageCode = TargetLanguageBox.Text.Trim();
        _settings.WhisperModelPath = WhisperModelPathBox.Text.Trim();
        _settings.WhisperLanguage = string.IsNullOrWhiteSpace(WhisperLanguageBox.Text) ? null : WhisperLanguageBox.Text.Trim();
        _settings.LibreTranslateBaseUrl = LibreTranslateUrlBox.Text.Trim();
        _settingsService.Save(_settings);

        int? processId = null;
        if (_selectedAudioProcess is { } selectedAudio &&
            string.Equals(_settings.TargetProcessName, selectedAudio.ProcessName, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using Process selectedProcess = Process.GetProcessById(selectedAudio.ProcessId);
                if (!selectedProcess.HasExited)
                {
                    processId = selectedAudio.ProcessId;
                }
            }
            catch (ArgumentException)
            {
                // The audio session ended after discovery; fall back to resolving the saved process name.
            }
        }

        processId ??= ProcessLookup.FindProcessId(_settings.TargetProcessName);
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
                "EchoLingo puede preparar LibreTranslate en un entorno aislado dentro de tu usuario. Se necesita Python 3.13 y conexión a internet. La primera descarga de paquetes y modelos puede tardar varios minutos y ocupar bastante espacio. ¿Continuar?",
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

        if (_shuttingDown)
        {
            return;
        }
        StartButton.IsEnabled = false;
        StatusText.Text = "Iniciando captura de audio...";

        try
        {
            _captureSource = new ProcessLoopbackCapture(processId.Value);
            _captureSource.DataAvailable += OnCaptureAudio;
            Interlocked.Exchange(ref _lastAudioTicks, 0);
            Interlocked.Exchange(ref _audioPeak, 0);
            _captureStartedTicks = DateTime.UtcNow.Ticks;
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

            StatusText.Text = $"Traduciendo audio de {selectedAudioName()} -> {_settings.TargetLanguageCode}.";
            StopButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"No se pudo iniciar: {ex.Message}";

            if (_captureSource is not null)
            {
                _captureSource.DataAvailable -= OnCaptureAudio;
            }
            _pipeline = null;
            _captureSource?.Dispose();
            _captureSource = null;
            _transcriber?.Dispose();
            _transcriber = null;

            StartButton.IsEnabled = true;
        }
    }

    private string selectedAudioName() =>
        _selectedAudioProcess?.DisplayName ?? _settings.TargetProcessName;

    private async void OnStopClicked(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        StatusText.Text = "Deteniendo...";

        await StopPipelineIfRunningAsync();

        StatusText.Text = "Subtítulos detenidos.";
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
        StatusText.Text = "Cerrando EchoLingo...";

        await StopPipelineIfRunningAsync();
        _translationService.Dispose();

        Application.Current.Shutdown();
    }

    private async Task StopPipelineIfRunningAsync()
    {
        if (_captureSource is not null)
        {
            _captureSource.DataAvailable -= OnCaptureAudio;
        }
        Interlocked.Exchange(ref _audioPeak, 0);
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

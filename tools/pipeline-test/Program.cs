using CsTranslate.Audio;
using CsTranslate.Cleanup;
using CsTranslate.Core.Models;
using CsTranslate.Core.Pipeline;
using CsTranslate.Speech;
using CsTranslate.Translation;
using NAudio.Wave;

if (args.Length < 3)
{
    Console.WriteLine("Kullanim: pipeline-test <process-adi-veya-pid | self-play:dosya.wav> <model-yolu> <hedef-dil> [sure-saniye=30] [konusulan-dil]");
    return 1;
}

string processSpec = args[0];
string modelPath = args[1];
string targetLanguage = args[2];
int durationSeconds = args.Length > 3 && int.TryParse(args[3], out int parsed) ? parsed : 30;
string? whisperLanguage = args.Length > 4 ? args[4] : null;

int processId;
Task? selfPlayTask = null;
CancellationTokenSource selfPlayCts = new();

if (processSpec.StartsWith("self-play:", StringComparison.Ordinal))
{
    string[] playbackFiles = processSpec["self-play:".Length..].Split('+');
    processId = Environment.ProcessId;
    Console.WriteLine($"Kendi process'imi ({processId}) hedefleyip {playbackFiles.Length} dosyayi es zamanli dongude calacagim.");
    var playTasks = playbackFiles.Select(f => Task.Run(() => LoopPlaybackAsync(f, selfPlayCts.Token))).ToArray();
    selfPlayTask = Task.WhenAll(playTasks);
}
else
{
    int? resolved = ProcessLookup.FindProcessId(processSpec);
    if (resolved is null)
    {
        Console.WriteLine($"'{processSpec}' adinda calisan bir process bulunamadi.");
        return 1;
    }
    processId = resolved.Value;
}

var settings = new AppSettings
{
    TargetProcessName = processSpec,
    TargetLanguageCode = targetLanguage,
    WhisperModelPath = modelPath,
    WhisperLanguage = whisperLanguage,
    LibreTranslateBaseUrl = "http://localhost:5000",
};

var translationClient = new LibreTranslateClient(settings.LibreTranslateBaseUrl);
if (!await translationClient.IsHealthyAsync())
{
    Console.WriteLine($"LibreTranslate {settings.LibreTranslateBaseUrl} adresinde ulasilamiyor.");
    return 1;
}

Console.WriteLine($"Hedef: PID {processId}, {settings.TargetLanguageCode} diline ceviri, {durationSeconds} saniye calisacak.");

using IAudioCaptureSource capture = new ProcessLoopbackCapture(processId);
using ITranscriber transcriber = new WhisperTranscriber(modelPath, whisperLanguage);
var cleanup = new GlossaryCleanupService(Path.Combine(AppContext.BaseDirectory, "Rules"));

var pipeline = new TranslationPipeline(
    capture,
    transcriber,
    cleanup,
    translationClient,
    settings,
    onTranslatedLine: line => Console.WriteLine($"[{line.Timestamp:HH:mm:ss}] ({line.SourceLanguage}) {line.OriginalText}  =>  {line.TranslatedText}"),
    onError: ex => Console.WriteLine($"HATA: {ex}"));

pipeline.Start();
await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
await pipeline.StopAsync();

selfPlayCts.Cancel();
if (selfPlayTask is not null)
{
    try { await selfPlayTask; } catch (OperationCanceledException) { }
}

Console.WriteLine("Pipeline durduruldu.");
return 0;

static async Task LoopPlaybackAsync(string file, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        using var audioFile = new AudioFileReader(file);
        using var outputDevice = new WaveOutEvent();
        outputDevice.Init(audioFile);
        outputDevice.Play();
        while (outputDevice.PlaybackState == PlaybackState.Playing && !token.IsCancellationRequested)
        {
            await Task.Delay(200, CancellationToken.None);
        }
    }
}

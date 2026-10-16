using CsTranslate.Audio;
using NAudio.Wave;

if (args.Length < 1)
{
    Console.WriteLine("Kullanim: wav-dump <process-adi> [sure-saniye] [cikti.wav]");
    Console.WriteLine("Ornek:    wav-dump Discord.exe 15 capture.wav");
    return 1;
}

string processName = args[0];
int durationSeconds = args.Length > 1 && int.TryParse(args[1], out int parsedDuration) ? parsedDuration : 15;
string outputPath = args.Length > 2 ? args[2] : "capture.wav";

IAudioCaptureSource capture = StartCapture(processName);

using (capture)
{
    var waveFormat = capture.Format.IsFloat
        ? WaveFormat.CreateIeeeFloatWaveFormat(capture.Format.SampleRate, capture.Format.Channels)
        : new WaveFormat(capture.Format.SampleRate, capture.Format.BitsPerSample, capture.Format.Channels);
    using var writer = new WaveFileWriter(outputPath, waveFormat);

    capture.DataAvailable += (_, e) => writer.Write(e.Buffer, 0, e.BytesRecorded);

    Console.WriteLine($"Kayit basliyor: {durationSeconds} saniye -> {outputPath}");
    Thread.Sleep(TimeSpan.FromSeconds(durationSeconds));
    capture.Stop();

    Console.WriteLine($"Kayit tamamlandi: {writer.Length} bayt yazildi -> {Path.GetFullPath(outputPath)}");
}

return 0;

static IAudioCaptureSource StartCapture(string processName)
{
    int? processId = ProcessLookup.FindProcessId(processName);
    if (processId is null)
    {
        Console.WriteLine($"'{processName}' adinda calisan bir process bulunamadi. Sistem geneli (fallback) yakalamaya geciliyor.");
        var fallback = new WasapiSystemLoopbackCapture();
        fallback.Start();
        return fallback;
    }

    Console.WriteLine($"Hedef process bulundu: {processName} (PID {processId}). Process-loopback yakalama deneniyor...");
    try
    {
        var processCapture = new ProcessLoopbackCapture(processId.Value);
        processCapture.Start();
        return processCapture;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Process-loopback basarisiz oldu ({ex.GetType().Name}: {ex.Message}). Sistem geneli (fallback) yakalamaya geciliyor.");
        Console.WriteLine("Kalici cozum icin VB-Cable kurup hedef uygulamanin cikisini oraya yonlendirin.");
        var fallback = new WasapiSystemLoopbackCapture();
        fallback.Start();
        return fallback;
    }
}

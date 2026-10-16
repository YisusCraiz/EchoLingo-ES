using CsTranslate.Audio;
using CsTranslate.Speech;
using NAudio.Wave;

if (args.Length < 2)
{
    Console.WriteLine("Kullanim: stt-test <giris-wav> <model-yolu> [dil-kodu]");
    Console.WriteLine("  giris-wav olarak 'self-play:<dosya.wav>' verilirse, o dosya bu process icinde");
    Console.WriteLine("  caldirilir ve ProcessLoopbackCapture ile bu process'in kendi sesi es zamanli");
    Console.WriteLine("  olarak yakalanir (tool-cagrilari arasindaki gecikmeden etkilenmeyen,");
    Console.WriteLine("  deterministik bir uctan uca dogrulama).");
    Console.WriteLine("Ornek: stt-test self-play:tools/models/jfk.wav tools/models/ggml-base.bin en");
    return 1;
}

string inputSpec = args[0];
string modelPath = args[1];
string? language = args.Length > 2 ? args[2] : null;

Stream sourceWavStream;
if (inputSpec.StartsWith("self-play:", StringComparison.Ordinal))
{
    string playbackFile = inputSpec["self-play:".Length..];
    sourceWavStream = await PlayAndSelfCaptureAsync(playbackFile);
}
else
{
    sourceWavStream = File.OpenRead(inputSpec);
}

Console.WriteLine("Yeniden orneklem yapiliyor (16kHz mono)...");
using MemoryStream resampled = AudioResampler.ResampleToWhisperFormat(sourceWavStream);
sourceWavStream.Dispose();

Console.WriteLine($"Model yukleniyor: {modelPath}");
using ITranscriber transcriber = new WhisperTranscriber(modelPath, language);

Console.WriteLine("Transkript uretiliyor...");
bool anySegment = false;
await foreach (TranscriptSegment segment in transcriber.TranscribeAsync(resampled))
{
    anySegment = true;
    Console.WriteLine($"[{segment.Start:mm\\:ss}-{segment.End:mm\\:ss}] ({segment.Language}) {segment.Text}");
}

if (!anySegment)
{
    Console.WriteLine("(hic segment uretilmedi)");
}

return 0;

static async Task<Stream> PlayAndSelfCaptureAsync(string playbackFile)
{
    int selfPid = Environment.ProcessId;
    Console.WriteLine($"Kendi process'imi ({selfPid}) hedefleyerek yakalama basliyor, ardindan '{playbackFile}' calinacak...");

    using var capture = new ProcessLoopbackCapture(selfPid);
    var recordedStream = new MemoryStream();
    var waveFormat = new WaveFormat(capture.Format.SampleRate, capture.Format.BitsPerSample, capture.Format.Channels);
    var writer = new WaveFileWriter(new NAudio.Utils.IgnoreDisposeStream(recordedStream), waveFormat);
    capture.DataAvailable += (_, e) => writer.Write(e.Buffer, 0, e.BytesRecorded);

    capture.Start();
    // Give the capture pipeline a moment to fully spin up (event handle, capture
    // thread) before we start producing audio, since audio sessions created after
    // Start() are still captured but the very first few ms can be missed otherwise.
    await Task.Delay(300);

    using (var audioFile = new AudioFileReader(playbackFile))
    using (var outputDevice = new WaveOutEvent())
    {
        outputDevice.Init(audioFile);
        outputDevice.Play();
        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(200);
        }
    }

    await Task.Delay(300);
    capture.Stop();
    writer.Dispose();

    recordedStream.Position = 0;
    Console.WriteLine($"Kendi-yakalama tamamlandi: {recordedStream.Length} bayt.");
    return recordedStream;
}

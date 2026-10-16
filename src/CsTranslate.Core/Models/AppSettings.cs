namespace CsTranslate.Core.Models;

public sealed class OverlaySettings
{
    public double X { get; set; } = 100;
    public double Y { get; set; } = 700;
    public double Width { get; set; } = 480;
    public double Opacity { get; set; } = 0.85;
    public int MaxLines { get; set; } = 6;
}

public sealed class VadSettings
{
    public double ChunkSoftSeconds { get; set; } = 2.0;
    public double ChunkHardCapSeconds { get; set; } = 6.0;
    public double SilenceThresholdRms { get; set; } = 0.01;
}

public sealed class AppSettings
{
    public string TargetProcessName { get; set; } = "Discord.exe";
    public string TargetLanguageCode { get; set; } = "en";
    public string WhisperModelPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "models", "ggml-base.bin");
    public string? WhisperLanguage { get; set; } = null;
    public string LibreTranslateBaseUrl { get; set; } = "http://localhost:5000";
    public OverlaySettings Overlay { get; set; } = new();
    public VadSettings Vad { get; set; } = new();
}

using System.IO;
using CsTranslate.Core.Models;
using CsTranslate.Core.Settings;

namespace CsTranslate.Audio.Tests;

public class InterfaceLanguageSettingsTests
{
    [Fact]
    public void EnglishInterface_RoundTrips_WithoutChangingSpeechOrSubtitleLanguage()
    {
        string directory = Path.Combine(Path.GetTempPath(), "EchoLingo-settings-" + Guid.NewGuid());
        try
        {
            var service = new SettingsService(Path.Combine(directory, "settings.json"));
            service.Save(new AppSettings
            {
                InterfaceLanguage = "en",
                WhisperLanguage = "en",
                TargetLanguageCode = "es"
            });
            AppSettings loaded = service.Load();
            Assert.Equal("en", loaded.InterfaceLanguage);
            Assert.Equal("en", loaded.WhisperLanguage);
            Assert.Equal("es", loaded.TargetLanguageCode);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ExistingSettings_WithoutInterfaceLanguage_KeepSpanishAndTranslationPreferences()
    {
        string directory = Path.Combine(Path.GetTempPath(), "EchoLingo-settings-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "settings.json");
            File.WriteAllText(path, "{\"TargetLanguageCode\":\"es\",\"WhisperLanguage\":\"en\"}");
            AppSettings loaded = new SettingsService(path).Load();
            Assert.Equal("es", loaded.InterfaceLanguage);
            Assert.Equal("es", loaded.TargetLanguageCode);
            Assert.Equal("en", loaded.WhisperLanguage);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}

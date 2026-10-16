using CsTranslate.Translation;

namespace CsTranslate.Translation.Tests;

/// <summary>
/// Integration tests against a locally running LibreTranslate instance
/// (see docs/SETUP.md). Skipped automatically if the server isn't reachable,
/// so `dotnet test` for the rest of the solution doesn't require it.
/// </summary>
public class LibreTranslateClientTests
{
    private const string BaseUrl = "http://localhost:5000";

    private static async Task<LibreTranslateClient?> CreateClientIfServerAvailableAsync()
    {
        var client = new LibreTranslateClient(BaseUrl);
        return await client.IsHealthyAsync() ? client : null;
    }

    [SkippableFact]
    public async Task IsHealthyAsync_ReturnsTrue_WhenServerRunning()
    {
        LibreTranslateClient? client = await CreateClientIfServerAvailableAsync();
        Skip.If(client is null, $"LibreTranslate {BaseUrl} adresinde calismiyor - docs/SETUP.md'ye bakin.");

        Assert.True(await client!.IsHealthyAsync());
    }

    [SkippableFact]
    public async Task TranslateAsync_RussianToEnglish_ProducesNonEmptyTranslation()
    {
        LibreTranslateClient? client = await CreateClientIfServerAvailableAsync();
        Skip.If(client is null, $"LibreTranslate {BaseUrl} adresinde calismiyor - docs/SETUP.md'ye bakin.");

        TranslationResult result = await client!.TranslateAsync("Привет, как дела?", targetLanguage: "en", sourceLanguage: "ru");

        Assert.False(string.IsNullOrWhiteSpace(result.TranslatedText));
    }
}

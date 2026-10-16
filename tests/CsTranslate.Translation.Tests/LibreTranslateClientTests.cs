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

    [Fact]
    public async Task TranslateAsync_SameSourceAndTarget_ReturnsOriginalWithoutRequest()
    {
        var handler = new StubHandler(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new LibreTranslateClient(httpClient);

        TranslationResult result = await client.TranslateAsync("Yeah.", targetLanguage: "en", sourceLanguage: "en-US");

        Assert.Equal("Yeah.", result.TranslatedText);
        Assert.Equal("en-US", result.DetectedSourceLanguage);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task TranslateAsync_BadRequest_IncludesServerErrorBody()
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\\"error\\":\\"language pair unavailable\\"}"),
        };
        var handler = new StubHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new LibreTranslateClient(httpClient);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.TranslateAsync("hola", targetLanguage: "en", sourceLanguage: "es"));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("language pair unavailable", exception.Message);
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
    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(response);
        }
    }
}

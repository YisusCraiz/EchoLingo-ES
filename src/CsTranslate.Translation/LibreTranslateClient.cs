using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CsTranslate.Translation;

/// <summary>
/// Thin HTTP client for a locally self-hosted LibreTranslate server. There is no
/// official strongly-typed .NET client for LibreTranslate, so this is a small
/// hand-rolled wrapper - the dependency surface is just "a local HTTP server".
/// </summary>
public sealed class LibreTranslateClient : ITranslationClient
{
    private readonly HttpClient _httpClient;

    public LibreTranslateClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // LibreTranslateBaseUrl is a user-editable setting (see AppSettings). It
    // defaults to localhost, but nothing stops it from being pointed at a
    // remote/attacker-controlled host, so this client is written as if the
    // server on the other end were not trusted: a bounded timeout so a
    // non-responsive endpoint can't hang the pipeline indefinitely, and a
    // capped response buffer so a malicious/misbehaving server can't exhaust
    // memory by returning an unbounded response body (CWE-400).
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    private const long MaxResponseBytes = 10 * 1024 * 1024; // 10 MB

    public LibreTranslateClient(string baseUrl)
        : this(new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = DefaultTimeout,
            MaxResponseContentBufferSize = MaxResponseBytes,
        })
    {
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        string sourceLanguage = "auto",
        CancellationToken cancellationToken = default)
    {
        string normalizedSource = NormalizeLanguageCode(sourceLanguage);
        string normalizedTarget = NormalizeLanguageCode(targetLanguage);
        if (!string.Equals(normalizedSource, "auto", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            // LibreTranslate may not have a same-language model pair. If Whisper
            // already identified the target language, preserve the text directly.
            return new TranslationResult(text, sourceLanguage);
        }

        var request = new TranslateRequest(text, sourceLanguage, targetLanguage, "text");

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("translate", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (errorBody.Length > 2000)
            {
                errorBody = errorBody[..2000];
            }

            throw new HttpRequestException(
                $"LibreTranslate devolvió HTTP {(int)response.StatusCode} ({response.StatusCode}): {errorBody}",
                inner: null,
                statusCode: response.StatusCode);
        }

        TranslateResponse? payload = await response.Content.ReadFromJsonAsync<TranslateResponse>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("LibreTranslate bos yanit dondurdu.");
        }

        return new TranslationResult(payload.TranslatedText, payload.DetectedLanguage?.Language);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync("languages", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    private static string NormalizeLanguageCode(string language)
    {
        string trimmed = language.Trim();
        int separator = trimmed.IndexOfAny(['-', '_']);
        return separator >= 0 ? trimmed[..separator] : trimmed;
    }

    private sealed record TranslateRequest(
        [property: JsonPropertyName("q")] string Q,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("target")] string Target,
        [property: JsonPropertyName("format")] string Format);

    private sealed record TranslateResponse(
        [property: JsonPropertyName("translatedText")] string TranslatedText,
        [property: JsonPropertyName("detectedLanguage")] DetectedLanguage? DetectedLanguage);

    private sealed record DetectedLanguage(
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("language")] string Language);
}

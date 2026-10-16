namespace CsTranslate.Translation;

public sealed record TranslationResult(string TranslatedText, string? DetectedSourceLanguage);

public interface ITranslationClient
{
    Task<TranslationResult> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto", CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

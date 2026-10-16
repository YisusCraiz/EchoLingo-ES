namespace CsTranslate.Core.Models;

public sealed record TranslatedLine(string OriginalText, string TranslatedText, string? SourceLanguage, DateTimeOffset Timestamp);

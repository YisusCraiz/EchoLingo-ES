using CsTranslate.Cleanup;
using CsTranslate.Translation;

if (args.Length < 2)
{
    Console.WriteLine("Kullanim: translate-test <metin> <hedef-dil> [kaynak-dil-ipucu=auto] [libretranslate-url=http://localhost:5000]");
    Console.WriteLine("Ornek:    translate-test \"мы рашим би\" en ru");
    return 1;
}

string text = args[0];
string targetLanguage = args[1];
string sourceLanguageHint = args.Length > 2 ? args[2] : "auto";
string baseUrl = args.Length > 3 ? args[3] : "http://localhost:5000";

string rulesDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CsTranslate.Cleanup", "Rules");
rulesDirectory = Path.GetFullPath(rulesDirectory);

var cleanup = new GlossaryCleanupService(rulesDirectory);
string cleaned = cleanup.Clean(text, sourceLanguageHint == "auto" ? null : sourceLanguageHint);

Console.WriteLine($"Ham metin:      {text}");
Console.WriteLine($"Temizlenmis:     {cleaned}");

var client = new LibreTranslateClient(baseUrl);

if (!await client.IsHealthyAsync())
{
    Console.WriteLine($"LibreTranslate {baseUrl} adresinde ulasilamiyor.");
    return 1;
}

TranslationResult result = await client.TranslateAsync(cleaned, targetLanguage, sourceLanguageHint);

Console.WriteLine($"Ceviri ({result.DetectedSourceLanguage ?? sourceLanguageHint} -> {targetLanguage}): {result.TranslatedText}");

return 0;

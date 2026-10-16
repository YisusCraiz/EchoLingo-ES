using CsTranslate.Cleanup;

namespace CsTranslate.Cleanup.Tests;

public class GlossaryCleanupServiceTests
{
    private static string RulesDirectory => Path.Combine(AppContext.BaseDirectory, "Rules");

    [Fact]
    public void ShippedGlossariesAreFoundAndLoadable()
    {
        Assert.True(Directory.Exists(RulesDirectory), $"Beklenen kural klasoru bulunamadi: {RulesDirectory}");
        Assert.NotEmpty(Directory.EnumerateFiles(RulesDirectory, "*.json"));
    }

    [Theory]
    [InlineData("ru", "команда рашим би", "команда rush би")]
    [InlineData("ru", "мы отступаем на a", "мы fall back на a")]
    [InlineData("ru", "мы    плантуем", "мы planting")]
    public void RussianGlossary_CorrectsKnownCallouts(string languageHint, string input, string expected)
    {
        var service = new GlossaryCleanupService(RulesDirectory);

        string result = service.Clean(input, languageHint);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clean_WithNoMatchingLanguage_ReturnsTextUnchangedApartFromUniversalRules()
    {
        var service = new GlossaryCleanupService(RulesDirectory);

        string result = service.Clean("рашим би", "fr");

        Assert.Equal("рашим би", result);
    }

    [Fact]
    public void Clean_CollapsesExtraWhitespace()
    {
        var service = new GlossaryCleanupService(RulesDirectory);

        string result = service.Clean("мы   отступаем   на a", "ru");

        Assert.Equal("мы fall back на a", result);
    }
}

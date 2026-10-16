using System.Text.Json;
using System.Text.RegularExpressions;

namespace CsTranslate.Cleanup;

/// <summary>
/// Applies deterministic, rule-based corrections to raw STT transcripts before
/// translation - no LLM involved. Rules live in per-language JSON glossary files
/// so they can be extended without recompiling.
/// </summary>
public sealed class GlossaryCleanupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly List<CompiledGlossary> _glossaries = [];

    public GlossaryCleanupService(string rulesDirectory)
    {
        Load(rulesDirectory);
    }

    public void Load(string rulesDirectory)
    {
        _glossaries.Clear();

        if (!Directory.Exists(rulesDirectory))
        {
            return;
        }

        foreach (string path in Directory.EnumerateFiles(rulesDirectory, "*.json"))
        {
            GlossaryFile? glossary = JsonSerializer.Deserialize<GlossaryFile>(File.ReadAllText(path), JsonOptions);
            if (glossary is not null)
            {
                _glossaries.Add(CompiledGlossary.Compile(glossary));
            }
        }
    }

    /// <param name="languageHint">
    /// A language code (e.g. "ru") to select the matching glossary. Glossaries
    /// with Language "*" always apply regardless of the hint. If null/empty,
    /// every loaded glossary is applied.
    /// </param>
    public string Clean(string text, string? languageHint = null)
    {
        string result = text;

        foreach (CompiledGlossary glossary in _glossaries)
        {
            bool applies = string.IsNullOrEmpty(languageHint)
                || glossary.Language == "*"
                || string.Equals(glossary.Language, languageHint, StringComparison.OrdinalIgnoreCase);

            if (!applies)
            {
                continue;
            }

            foreach ((Regex regex, string replacement) in glossary.Replacements)
            {
                result = SafeReplace(regex, result, replacement);
            }

            foreach ((Regex regex, string replacement) in glossary.RegexRules)
            {
                result = SafeReplace(regex, result, replacement);
            }
        }

        return result;
    }

    /// <summary>
    /// Applies one rule's regex, tolerating catastrophic backtracking. Glossary
    /// JSON files are user/community-editable (see docs), so a pattern here is
    /// effectively untrusted input: without a match timeout, a pathological
    /// pattern (e.g. nested quantifiers) can hang this call indefinitely and,
    /// because Regex.Replace does not observe a CancellationToken, that hang
    /// cannot be cancelled by pipeline shutdown either. On timeout, the offending
    /// rule is skipped for this text rather than taking down the pipeline.
    /// </summary>
    private static string SafeReplace(Regex regex, string input, string replacement)
    {
        try
        {
            return regex.Replace(input, replacement);
        }
        catch (RegexMatchTimeoutException)
        {
            return input;
        }
    }

    private sealed record CompiledGlossary(
        string Language,
        List<(Regex Regex, string Replacement)> Replacements,
        List<(Regex Regex, string Replacement)> RegexRules)
    {
        public static CompiledGlossary Compile(GlossaryFile glossary)
        {
            var replacements = glossary.Replacements
                .Select(r => (Compile(r.Pattern, r.CaseInsensitive), r.Replacement))
                .ToList();

            var regexRules = glossary.RegexRules
                .Select(r => (Compile(r.Pattern, caseInsensitive: false), r.Replacement))
                .ToList();

            return new CompiledGlossary(glossary.Language, replacements, regexRules);
        }

        private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(200);

        private static Regex Compile(string pattern, bool caseInsensitive)
        {
            RegexOptions options = RegexOptions.Compiled;
            if (caseInsensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }

            // matchTimeout bounds catastrophic backtracking (CWE-1333) from a
            // glossary rule that ships with, or is added to, Rules/*.json.
            return new Regex(pattern, options, MatchTimeout);
        }
    }
}

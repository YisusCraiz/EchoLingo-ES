namespace CsTranslate.Cleanup;

public sealed class GlossaryFile
{
    public int Version { get; set; } = 1;
    public string Language { get; set; } = string.Empty;
    public List<ReplacementRule> Replacements { get; set; } = [];
    public List<RegexRule> RegexRules { get; set; } = [];
}

/// <summary>A literal/word-boundary replacement, e.g. correcting a mis-heard callout.</summary>
public sealed class ReplacementRule
{
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
    public bool CaseInsensitive { get; set; } = true;
    public string? Note { get; set; }
}

/// <summary>A free-form regex pattern rule, e.g. fixing spacing/punctuation artifacts.</summary>
public sealed class RegexRule
{
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
    public string? Note { get; set; }
}

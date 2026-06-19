namespace Publisher.Core.Models;

/// <summary>Request sent to the AI Gateway's POST /v1/generate. Mirrors the service contract.</summary>
public sealed class AIGenerateRequest
{
    /// <summary>article | product | news | social.</summary>
    public string ContentType { get; set; } = "article";

    /// <summary>Required unless <see cref="UserPrompt"/> is provided.</summary>
    public string? Topic { get; set; }

    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();
    public int Length { get; set; } = 800;
    public string Tone { get; set; } = "seo-friendly";

    /// <summary>Source language (BCP-47/ISO code), e.g. "vi".</summary>
    public string Language { get; set; } = "vi";

    /// <summary>Optional system prompt (e.g. rendered from a template).</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Optional fully-rendered user prompt; overrides topic/keywords when set.</summary>
    public string? UserPrompt { get; set; }

    /// <summary>Target languages to translate into. Empty = original only.</summary>
    public IReadOnlyList<string> TranslateTo { get; set; } = Array.Empty<string>();

    /// <summary>Optional model override; gateway uses its default when null.</summary>
    public string? Model { get; set; }
}

/// <summary>A generated (or translated) article in one language.</summary>
public sealed class AIArticle
{
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
}

/// <summary>Token usage reported by the gateway/provider.</summary>
public sealed class AIUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}

/// <summary>Response from POST /v1/generate.</summary>
public sealed class AIGenerateResult
{
    /// <summary>Which provider actually produced the original (codex | claude | gemini).</summary>
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public AIArticle Original { get; set; } = new();
    public IReadOnlyList<AIArticle> Translations { get; set; } = Array.Empty<AIArticle>();
    public AIUsage Usage { get; set; } = new();
    public int DurationMs { get; set; }
    public string RequestId { get; set; } = string.Empty;
}

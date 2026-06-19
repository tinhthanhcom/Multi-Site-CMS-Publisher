using Publisher.Core.Entities;

namespace Publisher.Infrastructure.AI;

/// <summary>Variables available to a prompt template.</summary>
public sealed record PromptVars(
    string Topic,
    IReadOnlyList<string> Keywords,
    int Length,
    string Tone,
    string SiteName);

/// <summary>
/// Renders an <see cref="AIPromptTemplate"/> by substituting the placeholder tokens
/// {topic}, {keywords}, {length}, {tone}, {site_name} in the system and user prompts.
/// </summary>
public static class PromptTemplateRenderer
{
    public static (string? SystemPrompt, string UserPrompt) Render(AIPromptTemplate template, PromptVars vars)
    {
        var system = Substitute(template.SystemPrompt, vars);
        var user = Substitute(template.UserPromptTpl, vars) ?? string.Empty;
        return (system, user);
    }

    /// <summary>Substitutes tokens in a single string. Returns null/empty unchanged.</summary>
    public static string? Substitute(string? text, PromptVars vars)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text
            .Replace("{topic}", vars.Topic ?? string.Empty, StringComparison.Ordinal)
            .Replace("{keywords}", string.Join(", ", vars.Keywords), StringComparison.Ordinal)
            .Replace("{length}", vars.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{tone}", vars.Tone ?? string.Empty, StringComparison.Ordinal)
            .Replace("{site_name}", vars.SiteName ?? string.Empty, StringComparison.Ordinal);
    }
}

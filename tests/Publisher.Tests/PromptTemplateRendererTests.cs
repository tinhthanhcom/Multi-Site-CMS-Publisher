using Publisher.Core.Entities;
using Publisher.Infrastructure.AI;

namespace Publisher.Tests;

public class PromptTemplateRendererTests
{
    private static AIPromptTemplate Template() => new()
    {
        SystemPrompt = "You write for {site_name} in a {tone} tone.",
        UserPromptTpl = "Write about {topic}. Keywords: {keywords}. Length: {length} words.",
        DefaultLength = 800,
        DefaultTone = "seo-friendly",
    };

    [Fact]
    public void Render_substitutes_all_tokens()
    {
        var vars = new PromptVars("cà phê", new[] { "sức khỏe", "tỉnh táo" }, 600, "vui vẻ", "Demo Site");
        var (system, user) = PromptTemplateRenderer.Render(Template(), vars);

        Assert.Equal("You write for Demo Site in a vui vẻ tone.", system);
        Assert.Equal("Write about cà phê. Keywords: sức khỏe, tỉnh táo. Length: 600 words.", user);
    }

    [Fact]
    public void Substitute_joins_keywords_with_comma_space()
    {
        var vars = new PromptVars("t", new[] { "a", "b", "c" }, 100, "x", "S");
        Assert.Equal("a, b, c", PromptTemplateRenderer.Substitute("{keywords}", vars));
    }

    [Fact]
    public void Substitute_empty_keywords_yields_empty()
    {
        var vars = new PromptVars("t", System.Array.Empty<string>(), 100, "x", "S");
        Assert.Equal("Keywords: .", PromptTemplateRenderer.Substitute("Keywords: {keywords}.", vars));
    }

    [Fact]
    public void Substitute_returns_null_for_null_template()
    {
        var vars = new PromptVars("t", System.Array.Empty<string>(), 100, "x", "S");
        Assert.Null(PromptTemplateRenderer.Substitute(null, vars));
    }

    [Fact]
    public void Substitute_leaves_unknown_tokens_intact()
    {
        var vars = new PromptVars("t", System.Array.Empty<string>(), 100, "x", "S");
        Assert.Equal("{unknown} t", PromptTemplateRenderer.Substitute("{unknown} {topic}", vars));
    }
}

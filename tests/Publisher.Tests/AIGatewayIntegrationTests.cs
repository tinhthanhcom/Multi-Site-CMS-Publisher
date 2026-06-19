using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Publisher.Core.Models;
using Publisher.Core.Options;
using Publisher.Infrastructure.AI;

namespace Publisher.Tests;

/// <summary>
/// Hits a REAL running AI Gateway. Skipped unless AIGATEWAY_TEST_URL is set.
/// For local E2E: start the gateway with MOCK_PROVIDER=true PROVIDER_ORDER=mock,
/// then run with env AIGATEWAY_TEST_URL=http://localhost:8080 AIGATEWAY_TEST_KEY=localtest.
/// </summary>
[Trait("Category", "Integration")]
public class AIGatewayIntegrationTests
{
    private static (string url, string key)? Target()
    {
        var url = Environment.GetEnvironmentVariable("AIGATEWAY_TEST_URL");
        var key = Environment.GetEnvironmentVariable("AIGATEWAY_TEST_KEY") ?? "localtest";
        return string.IsNullOrWhiteSpace(url) ? null : (url, key);
    }

    [Fact]
    public async Task GenerateAsync_against_running_gateway_returns_article_and_translations()
    {
        var target = Target();
        if (target is null) return; // not configured — treat as skipped

        var opts = new AIGatewayOptions { BaseUrl = target.Value.url, ApiKey = target.Value.key, Enabled = true };
        var baseUrl = opts.BaseUrl.EndsWith('/') ? opts.BaseUrl : opts.BaseUrl + "/";
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };
        var svc = new AIContentService(http, Options.Create(opts), NullLogger<AIContentService>.Instance);

        var result = await svc.GenerateAsync(new AIGenerateRequest
        {
            Topic = "Lợi ích của cà phê",
            Keywords = new[] { "cà phê", "sức khỏe" },
            Length = 500,
            Tone = "thân thiện",
            Language = "vi",
            TranslateTo = new[] { "en", "ja" },
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Provider));
        Assert.False(string.IsNullOrWhiteSpace(result.Original.Title));
        Assert.False(string.IsNullOrWhiteSpace(result.Original.Content));
        Assert.Equal(2, result.Translations.Count);
        Assert.Contains(result.Translations, t => t.Language == "en");
        Assert.Contains(result.Translations, t => t.Language == "ja");
        Assert.True(result.Usage.TotalTokens > 0);
    }
}

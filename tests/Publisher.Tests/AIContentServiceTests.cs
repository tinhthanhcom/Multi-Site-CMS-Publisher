using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Publisher.Core.Models;
using Publisher.Core.Options;
using Publisher.Infrastructure.AI;

namespace Publisher.Tests;

public class AIContentServiceTests
{
    /// <summary>Captures the outgoing request and returns a canned response.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        public CapturingHandler(HttpStatusCode status, string responseBody)
        {
            _status = status;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static AIContentService Build(CapturingHandler handler, AIGatewayOptions? opts = null)
    {
        opts ??= new AIGatewayOptions { BaseUrl = "https://ai.example.com", ApiKey = "secret", Enabled = true };
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.com/") };
        return new AIContentService(http, Options.Create(opts), NullLogger<AIContentService>.Instance);
    }

    private const string OkResponse = """
    {
      "provider": "codex",
      "model": "gpt-5.4",
      "original": { "language": "vi", "title": "Tiêu đề", "content": "<p>Nội dung</p>", "excerpt": "Tóm tắt" },
      "translations": [ { "language": "en", "title": "Title", "content": "<p>Body</p>", "excerpt": "Sum" } ],
      "usage": { "inputTokens": 100, "outputTokens": 50, "totalTokens": 150 },
      "durationMs": 1234,
      "requestId": "abc"
    }
    """;

    [Fact]
    public async Task GenerateAsync_sends_bearer_and_camelCase_body()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, OkResponse);
        var svc = Build(handler);

        await svc.GenerateAsync(new AIGenerateRequest
        {
            Topic = "cà phê",
            Keywords = new[] { "sức khỏe" },
            TranslateTo = new[] { "en" },
        });

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("secret", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.EndsWith("/v1/generate", handler.LastRequest.RequestUri!.AbsoluteUri);
        Assert.Contains("\"contentType\"", handler.LastBody);
        Assert.Contains("\"translateTo\"", handler.LastBody);
        Assert.Contains("\"keywords\"", handler.LastBody);
        Assert.Contains("\"topic\"", handler.LastBody);
    }

    [Fact]
    public async Task GenerateAsync_parses_successful_response()
    {
        var svc = Build(new CapturingHandler(HttpStatusCode.OK, OkResponse));

        var result = await svc.GenerateAsync(new AIGenerateRequest { Topic = "x" });

        Assert.Equal("codex", result.Provider);
        Assert.Equal("Tiêu đề", result.Original.Title);
        Assert.Single(result.Translations);
        Assert.Equal("en", result.Translations[0].Language);
        Assert.Equal(150, result.Usage.TotalTokens);
    }

    [Fact]
    public async Task GenerateAsync_maps_error_envelope_to_exception()
    {
        const string errBody = """{ "error": { "code": "all_providers_failed", "message": "boom", "provider": "codex" } }""";
        var svc = Build(new CapturingHandler(HttpStatusCode.BadGateway, errBody));

        var ex = await Assert.ThrowsAsync<AIGatewayException>(() => svc.GenerateAsync(new AIGenerateRequest { Topic = "x" }));
        Assert.Equal("all_providers_failed", ex.Code);
        Assert.Equal("codex", ex.Provider);
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_throws_when_not_configured()
    {
        // No BaseUrl/key => IsConfigured false.
        var opts = new AIGatewayOptions { BaseUrl = "", ApiKey = "", Enabled = true };
        var http = new HttpClient(new CapturingHandler(HttpStatusCode.OK, OkResponse)); // no BaseAddress
        var svc = new AIContentService(http, Options.Create(opts), NullLogger<AIContentService>.Instance);

        var ex = await Assert.ThrowsAsync<AIGatewayException>(() => svc.GenerateAsync(new AIGenerateRequest { Topic = "x" }));
        Assert.Equal("not_configured", ex.Code);
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Publisher.Core.Interfaces;
using Publisher.Core.Models;
using Publisher.Core.Options;

namespace Publisher.Infrastructure.AI;

/// <summary>
/// Typed HttpClient calling the external AI Gateway (POST /v1/generate).
/// The BaseAddress/timeout are configured in DI; the bearer key is attached per request.
/// </summary>
public sealed class AIContentService : IAIContentService
{
    // Web defaults => camelCase naming + case-insensitive, matching the gateway's JSON.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AIGatewayOptions _options;
    private readonly ILogger<AIContentService> _logger;

    public AIContentService(HttpClient http, IOptions<AIGatewayOptions> options, ILogger<AIContentService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AIGenerateResult> GenerateAsync(AIGenerateRequest request, CancellationToken ct = default)
    {
        if (!_options.IsConfigured || _http.BaseAddress is null)
        {
            throw new AIGatewayException("AI Gateway chưa được cấu hình (thiếu URL hoặc API key).", code: "not_configured");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/generate")
        {
            Content = JsonContent.Create(request, options: Json),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AIGatewayException("AI Gateway phản hồi quá thời gian chờ.", code: "timeout", inner: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AIGatewayException($"Không kết nối được AI Gateway: {ex.Message}", code: "connection", inner: ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var (code, msg, provider) = await ReadErrorAsync(response, ct);
                _logger.LogWarning("AI Gateway error {Status} {Code} (provider={Provider})", (int)response.StatusCode, code, provider);
                throw new AIGatewayException(msg, code, provider);
            }

            var result = await response.Content.ReadFromJsonAsync<AIGenerateResult>(Json, ct);
            return result ?? throw new AIGatewayException("AI Gateway trả về phản hồi rỗng.", code: "empty_response");
        }
    }

    private static async Task<(string? code, string message, string? provider)> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<GatewayErrorEnvelope>(Json, ct);
            if (body?.Error is { } e && !string.IsNullOrWhiteSpace(e.Message))
            {
                return (e.Code, e.Message, e.Provider);
            }
        }
        catch
        {
            // Non-JSON error body — fall through to a generic message.
        }
        return (null, $"AI Gateway lỗi (HTTP {(int)response.StatusCode}).", null);
    }

    private sealed class GatewayErrorEnvelope
    {
        public GatewayError? Error { get; set; }
    }

    private sealed class GatewayError
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public string? Provider { get; set; }
    }
}

namespace Publisher.Core.Options;

/// <summary>
/// Configuration for the external AI Gateway service.
/// Bound from config section "AIGateway"; URL/key can be overridden by env vars
/// PUBLISHER_AIGATEWAY_URL / PUBLISHER_AIGATEWAY_KEY (preferred in production).
/// </summary>
public sealed class AIGatewayOptions
{
    public const string SectionName = "AIGateway";

    /// <summary>Base URL of the gateway, e.g. https://ai.example.com.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Bearer token sent as Authorization header.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Optional default model passed through to the gateway.</summary>
    public string? DefaultModel { get; set; }

    /// <summary>HTTP timeout (seconds). Generation can be slow.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Master switch; when false the AI features are hidden/disabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>True only when a URL and key are present and the feature is enabled.</summary>
    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
}

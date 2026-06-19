using Publisher.Core.Models;

namespace Publisher.Core.Interfaces;

/// <summary>
/// Client for the external AI Gateway. Sends a generation request and returns the
/// produced article (plus any requested translations). The gateway handles provider
/// fallback (codex → Claude → Gemini) internally.
/// </summary>
public interface IAIContentService
{
    Task<AIGenerateResult> GenerateAsync(AIGenerateRequest request, CancellationToken ct = default);
}

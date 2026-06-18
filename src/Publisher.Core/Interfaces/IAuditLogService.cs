namespace Publisher.Core.Interfaces;

/// <summary>Writes audit log entries. Implementations must never throw out to callers in a
/// way that breaks the main flow (swallow + self-log failures).</summary>
public interface IAuditLogService
{
    Task WriteAsync(
        string action,
        int? userId = null,
        int? siteId = null,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        bool isSuccess = true,
        string? errorMessage = null,
        int? durationMs = null,
        string? ipAddress = null,
        CancellationToken ct = default);
}

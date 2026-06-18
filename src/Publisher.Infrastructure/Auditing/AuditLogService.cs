using Microsoft.Extensions.Logging;
using Publisher.Core.Entities;
using Publisher.Core.Interfaces;
using Publisher.Infrastructure.Data;

namespace Publisher.Infrastructure.Auditing;

/// <summary>
/// Writes audit log entries via EF Core. Never throws out to callers — failures are
/// swallowed and self-logged so audit failures cannot break the main flow.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task WriteAsync(
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
        CancellationToken ct = default)
    {
        try
        {
            var entry = new AuditLog
            {
                Action = action,
                UserId = userId,
                SiteId = siteId,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                DurationMs = durationMs,
                IpAddress = ipAddress
                // CreatedAt left to DB default (GETUTCDATE()).
            };

            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Audit logging must never break the main flow.
            _logger.LogError(ex, "Failed to write audit log entry for action {Action}", action);
        }
    }
}

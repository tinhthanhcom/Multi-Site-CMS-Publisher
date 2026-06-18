namespace Publisher.Core.Entities;

/// <summary>Maps to table dbo.AuditLogs.</summary>
public class AuditLog
{
    public long Id { get; set; }
    /// <summary>NULL = system/scheduler.</summary>
    public int? UserId { get; set; }
    public int? SiteId { get; set; }
    /// <summary>POST_PUBLISHED | SITE_CREATED | CONFIG_CHANGED | AI_GENERATED | LOGIN | ...</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Posts | Sites | SiteFieldMappings | ...</summary>
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    /// <summary>JSON details.</summary>
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public int? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

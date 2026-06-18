namespace Publisher.Core.Entities;

/// <summary>Maps to table dbo.AutoPublishSchedules.</summary>
public class AutoPublishSchedule
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // AI Config
    public int? PromptTemplateId { get; set; }
    /// <summary>JSON array: ["topic1", "topic2", ...].</summary>
    public string TopicsJson { get; set; } = string.Empty;
    public string? KeywordsJson { get; set; }

    // Schedule Config
    /// <summary>daily | weekly | cron (CK_AutoSchedules_Type).</summary>
    public string ScheduleType { get; set; } = "daily";
    public string? CronExpression { get; set; }
    public TimeSpan? TimeOfDay { get; set; }
    /// <summary>0=Sun, 1=Mon, ... 6=Sat (for weekly).</summary>
    public byte? DayOfWeek { get; set; }
    public int PostsPerRun { get; set; } = 1;

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    /// <summary>success | failed | partial.</summary>
    public string? LastRunStatus { get; set; }
    public DateTime? NextRunAt { get; set; }
    public int TotalPostsPublished { get; set; }

    // Audit
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Site? Site { get; set; }
    public AIPromptTemplate? PromptTemplate { get; set; }
    public User? CreatedByUser { get; set; }
}

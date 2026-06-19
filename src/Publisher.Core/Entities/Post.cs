namespace Publisher.Core.Entities;

/// <summary>Maps to table dbo.Posts.</summary>
public class Post
{
    public int Id { get; set; }
    public int SiteId { get; set; }

    // Content
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? Thumbnail { get; set; }
    public string? CategoryId { get; set; }
    public string? AuthorId { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? CustomDataJson { get; set; }

    // Multi-language
    /// <summary>BCP-47/ISO language code of this variant (e.g. "vi", "en").</summary>
    public string Language { get; set; } = "vi";
    /// <summary>Groups the language variants of one logical post. NULL = standalone (single language).</summary>
    public Guid? TranslationGroupId { get; set; }

    // Status
    /// <summary>'draft' | 'scheduled' | 'publishing' | 'published' | 'failed' (CK_Posts_Status).</summary>
    public string Status { get; set; } = Enums.PostStatuses.Draft;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    // Publish result
    public string? RemotePostId { get; set; }
    public string? PublishError { get; set; }
    public int RetryCount { get; set; }

    // AI
    public bool IsAIGenerated { get; set; }
    public string? AIPromptUsed { get; set; }
    public int? AITokensUsed { get; set; }

    // Audit
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Site? Site { get; set; }
    public User? CreatedByUser { get; set; }
}

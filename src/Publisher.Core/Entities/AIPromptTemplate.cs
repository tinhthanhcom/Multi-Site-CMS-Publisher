namespace Publisher.Core.Entities;

/// <summary>Maps to table dbo.AIPromptTemplates. SiteId NULL = shared across all sites.</summary>
public class AIPromptTemplate
{
    public int Id { get; set; }
    /// <summary>NULL = shared/global template.</summary>
    public int? SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>article | product | news | social.</summary>
    public string ContentType { get; set; } = "article";
    public string? SystemPrompt { get; set; }
    /// <summary>Template with variables {topic}, {keywords}, {length}, {tone}.</summary>
    public string UserPromptTpl { get; set; } = string.Empty;
    public int DefaultLength { get; set; } = 800;
    public string DefaultTone { get; set; } = "seo-friendly";
    public bool IsActive { get; set; } = true;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Site? Site { get; set; }
    public User? CreatedByUser { get; set; }
}

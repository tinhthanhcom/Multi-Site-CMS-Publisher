namespace Publisher.Core.Entities;

/// <summary>Maps to table dbo.Sites.</summary>
public class Site
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string? Description { get; set; }

    // Connection
    /// <summary>AES-256-GCM encrypted connection string (see IConnectionStringEncryptor).</summary>
    public string ConnectionStringEnc { get; set; } = string.Empty;
    /// <summary>'SqlServer' | 'MySQL' (CK_Sites_DbType).</summary>
    public string DbType { get; set; } = "SqlServer";

    // AI Prompt
    public string? SystemPrompt { get; set; }
    public string? DefaultTone { get; set; }
    public string DefaultLanguage { get; set; } = "vi";

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? LastConnectionTest { get; set; }
    /// <summary>NULL = not tested, true = OK, false = fail.</summary>
    public bool? LastConnectionStatus { get; set; }
    public string? LastConnectionError { get; set; }

    // Audit
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User? CreatedByUser { get; set; }
    public SiteFieldMapping? FieldMapping { get; set; }
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<AIPromptTemplate> PromptTemplates { get; set; } = new List<AIPromptTemplate>();
    public ICollection<AutoPublishSchedule> Schedules { get; set; } = new List<AutoPublishSchedule>();
}

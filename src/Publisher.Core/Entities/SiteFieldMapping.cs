namespace Publisher.Core.Entities;

/// <summary>Maps to table dbo.SiteFieldMappings. One mapping per site (UQ_SiteFieldMappings_Site).</summary>
public class SiteFieldMapping
{
    public int Id { get; set; }
    public int SiteId { get; set; }

    // Target table
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";

    // Required fields
    public string FieldTitle { get; set; } = string.Empty;
    public string FieldContent { get; set; } = string.Empty;
    public string FieldStatus { get; set; } = string.Empty;

    // Status values
    public string StatusValueDraft { get; set; } = "0";
    public string StatusValuePublished { get; set; } = "1";

    // Optional fields (NULL = not mapped)
    public string? FieldSlug { get; set; }
    public string? FieldExcerpt { get; set; }
    public string? FieldThumbnail { get; set; }
    public string? FieldPublishedAt { get; set; }
    public string? FieldCategoryId { get; set; }
    public string? FieldAuthorId { get; set; }
    public string? FieldSortOrder { get; set; }
    public string? FieldSeoTitle { get; set; }
    public string? FieldSeoDescription { get; set; }

    // Default values
    public string? DefaultAuthorId { get; set; }
    public string? DefaultCategoryId { get; set; }

    /// <summary>JSON array of <see cref="Models.CustomFieldDef"/>.</summary>
    public string? CustomFieldsJson { get; set; }

    // Audit
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Site? Site { get; set; }
    public User? CreatedByUser { get; set; }
}

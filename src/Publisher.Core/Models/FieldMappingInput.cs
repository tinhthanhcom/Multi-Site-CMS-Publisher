namespace Publisher.Core.Models;

/// <summary>
/// Plain DTO mirroring the editable fields of <see cref="Entities.SiteFieldMapping"/>.
/// Used by the mapping UI and the INSERT builder. Audit columns (Id, SiteId, CreatedBy,
/// timestamps) are intentionally excluded — they are set by the service layer.
/// </summary>
public sealed class FieldMappingInput
{
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";

    // Required fields
    public string FieldTitle { get; set; } = string.Empty;
    public string FieldContent { get; set; } = string.Empty;
    public string FieldStatus { get; set; } = string.Empty;

    // Status values
    public string StatusValueDraft { get; set; } = "0";
    public string StatusValuePublished { get; set; } = "1";

    // Optional fields
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

    /// <summary>JSON array of <see cref="CustomFieldDef"/>.</summary>
    public string? CustomFieldsJson { get; set; }

    /// <summary>
    /// Per-language column names for localized fields (Title/Content/Excerpt/Slug/SeoTitle/SeoDescription),
    /// JSON: { "Title": {"vi":"title_vi","en":"title_en"}, ... }. NULL/empty = single-language.
    /// </summary>
    public string? LocalizedColumnsJson { get; set; }
}

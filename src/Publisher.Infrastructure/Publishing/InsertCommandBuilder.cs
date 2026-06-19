using System.Globalization;
using System.Text;
using System.Text.Json;
using Publisher.Core.Models;
using Publisher.Infrastructure.Security;

namespace Publisher.Infrastructure.Publishing;

/// <summary>
/// Result of building a parameterized INSERT. Pure data — no DB calls.
/// <para>
/// The <see cref="Sql"/> ends with a trailing <c>SELECT CAST(SCOPE_IDENTITY() AS NVARCHAR(50))</c>
/// so the publisher can read the new row id as a string (RemotePostId).
/// </para>
/// </summary>
public sealed record InsertBuildResult(string Sql, IReadOnlyDictionary<string, object?> Parameters)
{
    /// <summary>Projects to the UI-facing <see cref="InsertPreview"/> (SQL + parameter names).</summary>
    public InsertPreview ToPreview() => new()
    {
        Sql = Sql,
        ParameterNames = Parameters.Keys.ToList(),
    };
}

/// <summary>
/// Builds the shared, parameterized INSERT statement used by both the mapping-preview UI
/// (Agent D) and the publisher (Agent E). Stateless and pure: every identifier is validated
/// via <see cref="SafeIdentifier"/> and bracket-quoted; every value is passed as an
/// <c>@p{n}</c> parameter — values are never interpolated into the SQL text.
/// </summary>
public sealed class InsertCommandBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Builds an INSERT for the given mapping + post data.
    /// </summary>
    /// <param name="mapping">Field mapping (table/column names + status values + defaults).</param>
    /// <param name="post">The post data to publish.</param>
    /// <param name="published">If true uses the published status value and stamps PublishedAt; else draft.</param>
    /// <param name="publishTimeUtc">Value used for the mapped PublishedAt column when <paramref name="published"/> is true.</param>
    public InsertBuildResult Build(
        FieldMappingInput mapping,
        PostPublishData post,
        bool published,
        DateTime publishTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(post);

        var schema = string.IsNullOrWhiteSpace(mapping.SchemaName) ? "dbo" : mapping.SchemaName;
        var qualifiedTable = SafeIdentifier.QualifiedName(schema, mapping.TableName);

        var columns = new List<string>();
        var placeholders = new List<string>();
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        void Add(string columnName, string paramNameForError, object? value)
        {
            var validated = SafeIdentifier.Validate(columnName, paramNameForError);
            var p = "@p" + parameters.Count.ToString(CultureInfo.InvariantCulture);
            columns.Add($"[{validated}]");
            placeholders.Add(p);
            parameters[p] = value;
        }

        // --- Required columns ---
        Add(mapping.FieldTitle, nameof(mapping.FieldTitle), post.Title);
        Add(mapping.FieldContent, nameof(mapping.FieldContent), post.Content);
        Add(
            mapping.FieldStatus,
            nameof(mapping.FieldStatus),
            published ? mapping.StatusValuePublished : mapping.StatusValueDraft);

        // --- Optional columns (only when mapped) ---
        if (!string.IsNullOrWhiteSpace(mapping.FieldSlug))
            Add(mapping.FieldSlug, nameof(mapping.FieldSlug), post.Slug);

        if (!string.IsNullOrWhiteSpace(mapping.FieldExcerpt))
            Add(mapping.FieldExcerpt, nameof(mapping.FieldExcerpt), post.Excerpt);

        if (!string.IsNullOrWhiteSpace(mapping.FieldThumbnail))
            Add(mapping.FieldThumbnail, nameof(mapping.FieldThumbnail), post.Thumbnail);

        if (!string.IsNullOrWhiteSpace(mapping.FieldCategoryId))
            Add(mapping.FieldCategoryId, nameof(mapping.FieldCategoryId), post.CategoryId ?? mapping.DefaultCategoryId);

        if (!string.IsNullOrWhiteSpace(mapping.FieldAuthorId))
            Add(mapping.FieldAuthorId, nameof(mapping.FieldAuthorId), post.AuthorId ?? mapping.DefaultAuthorId);

        if (!string.IsNullOrWhiteSpace(mapping.FieldSeoTitle))
            Add(mapping.FieldSeoTitle, nameof(mapping.FieldSeoTitle), post.SeoTitle);

        if (!string.IsNullOrWhiteSpace(mapping.FieldSeoDescription))
            Add(mapping.FieldSeoDescription, nameof(mapping.FieldSeoDescription), post.SeoDescription);

        if (!string.IsNullOrWhiteSpace(mapping.FieldSortOrder))
            Add(mapping.FieldSortOrder, nameof(mapping.FieldSortOrder), null);

        if (!string.IsNullOrWhiteSpace(mapping.FieldPublishedAt))
        {
            // Only stamp the publish time when actually publishing; otherwise NULL.
            object? publishedAt = published ? publishTimeUtc : null;
            Add(mapping.FieldPublishedAt, nameof(mapping.FieldPublishedAt), publishedAt);
        }

        // --- Custom fields ---
        foreach (var def in ParseCustomFields(mapping.CustomFieldsJson))
        {
            Add(def.FieldName, "CustomField:" + def.FieldName, CoerceValue(def.DefaultValue, def.DataType));
        }

        var sql = new StringBuilder()
            .Append("INSERT INTO ").Append(qualifiedTable)
            .Append(" (").Append(string.Join(", ", columns)).Append(')')
            .Append(" VALUES (").Append(string.Join(", ", placeholders)).Append(");")
            .Append(" SELECT CAST(SCOPE_IDENTITY() AS NVARCHAR(50));")
            .ToString();

        return new InsertBuildResult(sql, parameters);
    }

    /// <summary>Localized field keys whose value comes from a per-language post.</summary>
    private static readonly string[] LocalizedFieldOrder =
        { "Title", "Content", "Excerpt", "Slug", "SeoTitle", "SeoDescription" };

    /// <summary>
    /// Builds a SINGLE INSERT for a translation group when the remote table stores languages as
    /// separate columns (e.g. title_vi, title_en). Localized fields are written to the column mapped
    /// for each language (from <see cref="FieldMappingInput.LocalizedColumnsJson"/>); common fields
    /// (status, thumbnail, category, author, publishedAt, sort, custom) come from the primary-language post.
    /// </summary>
    public InsertBuildResult BuildLocalized(
        FieldMappingInput mapping,
        IReadOnlyList<PostPublishData> posts,
        string primaryLanguage,
        bool published,
        DateTime publishTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(posts);
        if (posts.Count == 0)
            throw new ArgumentException("At least one post is required.", nameof(posts));

        var localized = ParseLocalizedColumns(mapping.LocalizedColumnsJson);
        var byLang = posts
            .GroupBy(p => p.Language ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var primary = byLang.TryGetValue(primaryLanguage ?? string.Empty, out var pp) ? pp : posts[0];

        var schema = string.IsNullOrWhiteSpace(mapping.SchemaName) ? "dbo" : mapping.SchemaName;
        var qualifiedTable = SafeIdentifier.QualifiedName(schema, mapping.TableName);

        var columns = new List<string>();
        var placeholders = new List<string>();
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        void Add(string columnName, string paramNameForError, object? value)
        {
            var validated = SafeIdentifier.Validate(columnName, paramNameForError);
            var p = "@p" + parameters.Count.ToString(CultureInfo.InvariantCulture);
            columns.Add($"[{validated}]");
            placeholders.Add(p);
            parameters[p] = value;
        }

        // --- Localized columns: one column per (field, language) that has both a mapping and content. ---
        foreach (var field in LocalizedFieldOrder)
        {
            if (!localized.TryGetValue(field, out var langCols)) continue;
            foreach (var lang in langCols.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                var column = langCols[lang];
                if (string.IsNullOrWhiteSpace(column)) continue;
                if (!byLang.TryGetValue(lang, out var langPost)) continue; // no content for this language
                Add(column, $"Localized:{field}:{lang}", GetLocalizedValue(field, langPost));
            }
        }

        // --- Common columns (single, from the primary-language post). ---
        Add(
            mapping.FieldStatus,
            nameof(mapping.FieldStatus),
            published ? mapping.StatusValuePublished : mapping.StatusValueDraft);

        if (!string.IsNullOrWhiteSpace(mapping.FieldThumbnail))
            Add(mapping.FieldThumbnail, nameof(mapping.FieldThumbnail), primary.Thumbnail);

        if (!string.IsNullOrWhiteSpace(mapping.FieldCategoryId))
            Add(mapping.FieldCategoryId, nameof(mapping.FieldCategoryId), primary.CategoryId ?? mapping.DefaultCategoryId);

        if (!string.IsNullOrWhiteSpace(mapping.FieldAuthorId))
            Add(mapping.FieldAuthorId, nameof(mapping.FieldAuthorId), primary.AuthorId ?? mapping.DefaultAuthorId);

        if (!string.IsNullOrWhiteSpace(mapping.FieldSortOrder))
            Add(mapping.FieldSortOrder, nameof(mapping.FieldSortOrder), null);

        if (!string.IsNullOrWhiteSpace(mapping.FieldPublishedAt))
            Add(mapping.FieldPublishedAt, nameof(mapping.FieldPublishedAt), published ? publishTimeUtc : (object?)null);

        foreach (var def in ParseCustomFields(mapping.CustomFieldsJson))
            Add(def.FieldName, "CustomField:" + def.FieldName, CoerceValue(def.DefaultValue, def.DataType));

        if (columns.Count == 0)
            throw new ArgumentException("No columns to insert — mapping has no localized columns mapped.");

        var sql = new StringBuilder()
            .Append("INSERT INTO ").Append(qualifiedTable)
            .Append(" (").Append(string.Join(", ", columns)).Append(')')
            .Append(" VALUES (").Append(string.Join(", ", placeholders)).Append(");")
            .Append(" SELECT CAST(SCOPE_IDENTITY() AS NVARCHAR(50));")
            .ToString();

        return new InsertBuildResult(sql, parameters);
    }

    private static object? GetLocalizedValue(string field, PostPublishData p) => field.ToLowerInvariant() switch
    {
        "title" => p.Title,
        "content" => p.Content,
        "excerpt" => p.Excerpt,
        "slug" => p.Slug,
        "seotitle" => p.SeoTitle,
        "seodescription" => p.SeoDescription,
        _ => null,
    };

    /// <summary>Parses LocalizedColumnsJson into field → (language → column), dropping blanks.</summary>
    public static Dictionary<string, Dictionary<string, string>> ParseLocalizedColumns(string? json)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
            return result;

        Dictionary<string, Dictionary<string, string>>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("LocalizedColumnsJson is not a valid JSON object.", nameof(json), ex);
        }

        if (parsed is null)
            return result;

        foreach (var (field, langCols) in parsed)
        {
            if (langCols is null) continue;
            var inner = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (lang, col) in langCols)
                if (!string.IsNullOrWhiteSpace(col)) inner[lang] = col;
            if (inner.Count > 0) result[field] = inner;
        }

        return result;
    }

    private static IEnumerable<CustomFieldDef> ParseCustomFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<CustomFieldDef>();

        List<CustomFieldDef>? defs;
        try
        {
            defs = JsonSerializer.Deserialize<List<CustomFieldDef>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("CustomFieldsJson is not a valid JSON array of CustomFieldDef.", nameof(json), ex);
        }

        return defs is null
            ? Array.Empty<CustomFieldDef>()
            : defs.Where(d => d is not null && !string.IsNullOrWhiteSpace(d.FieldName));
    }

    /// <summary>Coerces a custom field's string DefaultValue to a CLR value per its DataType.</summary>
    private static object? CoerceValue(string? rawValue, string? dataType)
    {
        if (rawValue is null)
            return null;

        switch ((dataType ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "int":
            case "integer":
            case "bigint":
                return long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                    ? l
                    : (object?)rawValue;

            case "decimal":
            case "float":
            case "double":
            case "money":
                return double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                    ? d
                    : (object?)rawValue;

            case "bit":
            case "bool":
            case "boolean":
                if (bool.TryParse(rawValue, out var b)) return b;
                if (rawValue == "1") return true;
                if (rawValue == "0") return false;
                return rawValue;

            case "datetime":
            case "datetime2":
            case "date":
                return DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt)
                    ? dt
                    : (object?)rawValue;

            default:
                return rawValue;
        }
    }
}

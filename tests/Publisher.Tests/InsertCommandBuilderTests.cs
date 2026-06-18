using Publisher.Core.Models;
using Publisher.Infrastructure.Publishing;
using Publisher.Infrastructure.Security;

namespace Publisher.Tests;

public class InsertCommandBuilderTests
{
    private static readonly DateTime PublishTime = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

    private static FieldMappingInput SampleMapping() => new()
    {
        SchemaName = "dbo",
        TableName = "Articles",
        FieldTitle = "Title",
        FieldContent = "Body",
        FieldStatus = "Status",
        StatusValueDraft = "0",
        StatusValuePublished = "1",
        FieldSlug = "Slug",
        FieldExcerpt = "Summary",
        FieldCategoryId = "CategoryId",
        FieldAuthorId = "AuthorId",
        FieldPublishedAt = "PublishedAt",
        DefaultAuthorId = "99",
        DefaultCategoryId = "5",
        CustomFieldsJson = "[{\"fieldName\":\"ViewCount\",\"defaultValue\":\"0\",\"dataType\":\"int\"}," +
                           "{\"fieldName\":\"IsFeatured\",\"defaultValue\":\"true\",\"dataType\":\"bit\"}]",
    };

    private static PostPublishData SamplePost() => new()
    {
        Title = "Hello World",
        Slug = "hello-world",
        Content = "<p>Body content</p>",
        Excerpt = "intro",
    };

    private readonly InsertCommandBuilder _builder = new();

    [Fact]
    public void Build_TargetsQualifiedTable_AndIncludesMappedColumns()
    {
        var result = _builder.Build(SampleMapping(), SamplePost(), published: true, PublishTime);

        Assert.Contains("INSERT INTO [dbo].[Articles]", result.Sql);
        Assert.Contains("[Title]", result.Sql);
        Assert.Contains("[Body]", result.Sql);
        Assert.Contains("[Status]", result.Sql);
        Assert.Contains("[Slug]", result.Sql);
        Assert.Contains("[ViewCount]", result.Sql);
        Assert.Contains("[IsFeatured]", result.Sql);
    }

    [Fact]
    public void Build_UsesOnlyParameterPlaceholders_NoRawValues()
    {
        var result = _builder.Build(SampleMapping(), SamplePost(), published: true, PublishTime);

        // No literal values from the post may appear in the SQL text.
        Assert.DoesNotContain("Hello World", result.Sql);
        Assert.DoesNotContain("hello-world", result.Sql);
        Assert.DoesNotContain("Body content", result.Sql);

        // VALUES clause uses only @p placeholders.
        var valuesStart = result.Sql.IndexOf("VALUES", StringComparison.Ordinal);
        var valuesClause = result.Sql[valuesStart..];
        Assert.All(result.Parameters.Keys, p => Assert.StartsWith("@p", p));
        Assert.Contains("@p0", valuesClause);
    }

    [Fact]
    public void Build_StatusReflectsPublishedFlag()
    {
        var publishedResult = _builder.Build(SampleMapping(), SamplePost(), published: true, PublishTime);
        var draftResult = _builder.Build(SampleMapping(), SamplePost(), published: false, PublishTime);

        // The status parameter is @p2 (Title, Content, Status order).
        Assert.Equal("1", publishedResult.Parameters["@p2"]);
        Assert.Equal("0", draftResult.Parameters["@p2"]);
    }

    [Fact]
    public void Build_OptionalFields_IncludedOnlyWhenMapped()
    {
        var mapping = SampleMapping();
        mapping.FieldThumbnail = null;   // not mapped
        mapping.FieldSeoTitle = null;    // not mapped

        var result = _builder.Build(mapping, SamplePost(), published: true, PublishTime);

        Assert.DoesNotContain("[Thumbnail]", result.Sql);
        Assert.Contains("[Slug]", result.Sql);
    }

    [Fact]
    public void Build_CategoryAndAuthor_FallBackToMappingDefaults()
    {
        var post = SamplePost();
        post.CategoryId = null; // should fall back to DefaultCategoryId = "5"
        post.AuthorId = null;   // should fall back to DefaultAuthorId = "99"

        var result = _builder.Build(SampleMapping(), post, published: true, PublishTime);

        Assert.Contains("5", result.Parameters.Values.Select(v => v?.ToString()));
        Assert.Contains("99", result.Parameters.Values.Select(v => v?.ToString()));
    }

    [Fact]
    public void Build_PublishedAt_StampedWhenPublished_NullWhenDraft()
    {
        var published = _builder.Build(SampleMapping(), SamplePost(), published: true, PublishTime);
        var draft = _builder.Build(SampleMapping(), SamplePost(), published: false, PublishTime);

        Assert.Contains(PublishTime, published.Parameters.Values.Cast<object?>());
        // In draft mode the PublishedAt parameter exists but is null.
        Assert.Contains(draft.Parameters.Values, v => v is null);
    }

    [Fact]
    public void Build_CustomFields_ParsedAndCoerced()
    {
        var result = _builder.Build(SampleMapping(), SamplePost(), published: true, PublishTime);

        Assert.Contains(0L, result.Parameters.Values.Cast<object?>());     // ViewCount int -> long 0
        Assert.Contains(true, result.Parameters.Values.Cast<object?>());   // IsFeatured bit -> true
    }

    [Fact]
    public void Build_EndsWithScopeIdentitySelect()
    {
        var result = _builder.Build(SampleMapping(), SamplePost(), published: true, PublishTime);
        Assert.Contains("SELECT CAST(SCOPE_IDENTITY() AS NVARCHAR(50));", result.Sql);
    }

    [Fact]
    public void ToPreview_ReturnsSqlAndParameterNames()
    {
        var result = _builder.Build(SampleMapping(), SamplePost(), published: true, PublishTime);
        var preview = result.ToPreview();

        Assert.Equal(result.Sql, preview.Sql);
        Assert.Equal(result.Parameters.Keys.ToList(), preview.ParameterNames);
    }

    [Fact]
    public void Build_Throws_ForUnsafeTableName()
    {
        var mapping = SampleMapping();
        mapping.TableName = "Articles; DROP TABLE Users";

        Assert.Throws<UnsafeIdentifierException>(
            () => _builder.Build(mapping, SamplePost(), published: true, PublishTime));
    }

    [Fact]
    public void Build_Throws_ForUnsafeColumnName()
    {
        var mapping = SampleMapping();
        mapping.FieldTitle = "Title]; --";

        Assert.Throws<UnsafeIdentifierException>(
            () => _builder.Build(mapping, SamplePost(), published: true, PublishTime));
    }

    [Fact]
    public void Build_Throws_ForUnsafeCustomFieldName()
    {
        var mapping = SampleMapping();
        mapping.CustomFieldsJson = "[{\"fieldName\":\"bad name\",\"defaultValue\":\"0\",\"dataType\":\"int\"}]";

        Assert.Throws<UnsafeIdentifierException>(
            () => _builder.Build(mapping, SamplePost(), published: true, PublishTime));
    }
}

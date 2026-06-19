using Publisher.Core.Models;
using Publisher.Infrastructure.Publishing;
using Publisher.Infrastructure.Security;

namespace Publisher.Tests;

public class InsertCommandBuilderLocalizedTests
{
    private static readonly DateTime PublishTime = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
    private readonly InsertCommandBuilder _builder = new();

    private static FieldMappingInput LocalizedMapping() => new()
    {
        SchemaName = "dbo",
        TableName = "Articles",
        // In localized mode FieldTitle/Content are ignored; status + commons stay single-column.
        FieldTitle = "ignored",
        FieldContent = "ignored",
        FieldStatus = "status",
        StatusValueDraft = "0",
        StatusValuePublished = "1",
        FieldCategoryId = "category_id",
        DefaultCategoryId = "5",
        LocalizedColumnsJson =
            "{\"Title\":{\"vi\":\"title_vi\",\"en\":\"title_en\"}," +
            "\"Content\":{\"vi\":\"content_vi\",\"en\":\"content_en\"}," +
            "\"Excerpt\":{\"vi\":\"excerpt_vi\",\"en\":\"excerpt_en\"}}",
    };

    private static PostPublishData Vi() => new()
    {
        Language = "vi",
        Title = "Xin chào",
        Content = "<p>noi dung vi</p>",
        Excerpt = "tom tat vi",
        CategoryId = "10",
    };

    private static PostPublishData En() => new()
    {
        Language = "en",
        Title = "Hello",
        Content = "<p>english body</p>",
        Excerpt = "summary en",
    };

    [Fact]
    public void BuildLocalized_EmitsPerLanguageColumns()
    {
        var result = _builder.BuildLocalized(LocalizedMapping(), new[] { Vi(), En() }, "vi", published: true, PublishTime);

        Assert.Contains("INSERT INTO [dbo].[Articles]", result.Sql);
        foreach (var col in new[] { "[title_vi]", "[title_en]", "[content_vi]", "[content_en]", "[excerpt_vi]", "[excerpt_en]", "[status]" })
            Assert.Contains(col, result.Sql);

        // Values present as parameters, not inlined.
        var values = result.Parameters.Values.Select(v => v?.ToString()).ToList();
        Assert.Contains("Xin chào", values);
        Assert.Contains("Hello", values);
        Assert.DoesNotContain("Xin chào", result.Sql);
        Assert.DoesNotContain("Hello", result.Sql);
    }

    [Fact]
    public void BuildLocalized_SkipsLanguageWithNoContent()
    {
        // Only Vietnamese provided; English columns mapped but no en post.
        var result = _builder.BuildLocalized(LocalizedMapping(), new[] { Vi() }, "vi", published: true, PublishTime);

        Assert.Contains("[title_vi]", result.Sql);
        Assert.DoesNotContain("[title_en]", result.Sql);
        Assert.DoesNotContain("[content_en]", result.Sql);
    }

    [Fact]
    public void BuildLocalized_StatusReflectsPublishedFlag_AndCommonFromPrimary()
    {
        var published = _builder.BuildLocalized(LocalizedMapping(), new[] { Vi(), En() }, "vi", published: true, PublishTime);
        var draft = _builder.BuildLocalized(LocalizedMapping(), new[] { Vi(), En() }, "vi", published: false, PublishTime);

        Assert.Contains("1", published.Parameters.Values.Select(v => v?.ToString()));
        Assert.Contains("0", draft.Parameters.Values.Select(v => v?.ToString()));
        // category from the primary (vi) post = "10", not the default "5".
        Assert.Contains("10", published.Parameters.Values.Select(v => v?.ToString()));
    }

    [Fact]
    public void BuildLocalized_CategoryFallsBackToDefault_WhenPrimaryNull()
    {
        var vi = Vi();
        vi.CategoryId = null;
        var result = _builder.BuildLocalized(LocalizedMapping(), new[] { vi, En() }, "vi", published: true, PublishTime);
        Assert.Contains("5", result.Parameters.Values.Select(v => v?.ToString())); // DefaultCategoryId
    }

    [Fact]
    public void BuildLocalized_EndsWithScopeIdentity_AndOnlyPlaceholders()
    {
        var result = _builder.BuildLocalized(LocalizedMapping(), new[] { Vi(), En() }, "vi", published: true, PublishTime);
        Assert.Contains("SELECT CAST(SCOPE_IDENTITY() AS NVARCHAR(50));", result.Sql);
        Assert.All(result.Parameters.Keys, p => Assert.StartsWith("@p", p));
    }

    [Fact]
    public void BuildLocalized_Throws_ForUnsafeLocalizedColumn()
    {
        var mapping = LocalizedMapping();
        mapping.LocalizedColumnsJson = "{\"Title\":{\"vi\":\"title_vi]; DROP TABLE x --\"}}";
        Assert.Throws<UnsafeIdentifierException>(
            () => _builder.BuildLocalized(mapping, new[] { Vi() }, "vi", published: true, PublishTime));
    }

    [Fact]
    public void ParseLocalizedColumns_DropsBlankColumns()
    {
        var map = InsertCommandBuilder.ParseLocalizedColumns(
            "{\"Title\":{\"vi\":\"title_vi\",\"en\":\"\"},\"Content\":{}}");
        Assert.True(map.ContainsKey("Title"));
        Assert.Single(map["Title"]);            // en dropped (blank)
        Assert.Equal("title_vi", map["Title"]["vi"]);
        Assert.False(map.ContainsKey("Content")); // empty inner dropped
    }
}

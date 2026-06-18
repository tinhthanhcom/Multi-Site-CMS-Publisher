using Publisher.Infrastructure.Security;

namespace Publisher.Tests;

public class SafeIdentifierTests
{
    [Theory]
    [InlineData("Posts")]
    [InlineData("dbo")]
    [InlineData("_hidden")]
    [InlineData("Field_1")]
    [InlineData("a")]
    [InlineData("ViewCount")]
    public void IsValid_ReturnsTrue_ForWhitelistedNames(string id)
    {
        Assert.True(SafeIdentifier.IsValid(id));
        // Validate should return the same string unchanged.
        Assert.Equal(id, SafeIdentifier.Validate(id, "test"));
    }

    [Theory]
    [InlineData("Posts; DROP TABLE x")]
    [InlineData("a b")]
    [InlineData("1abc")]
    [InlineData("col--")]
    [InlineData("")]
    [InlineData("[Posts]")]
    [InlineData("dbo.Posts")]
    [InlineData("col'")]
    [InlineData("t*")]
    public void IsValid_ReturnsFalse_ForInjectionInputs(string id)
    {
        Assert.False(SafeIdentifier.IsValid(id));
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForNull()
    {
        Assert.False(SafeIdentifier.IsValid(null));
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForTooLongName()
    {
        var tooLong = new string('a', SafeIdentifier.MaxLength + 1);
        Assert.False(SafeIdentifier.IsValid(tooLong));
        Assert.Throws<UnsafeIdentifierException>(() => SafeIdentifier.Validate(tooLong, "test"));
    }

    [Fact]
    public void IsValid_ReturnsTrue_AtMaxLength()
    {
        var maxLen = new string('a', SafeIdentifier.MaxLength);
        Assert.True(SafeIdentifier.IsValid(maxLen));
    }

    [Theory]
    [InlineData("Posts; DROP TABLE x")]
    [InlineData("a b")]
    [InlineData("1abc")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_Throws_ForInvalidInputs(string? id)
    {
        Assert.Throws<UnsafeIdentifierException>(() => SafeIdentifier.Validate(id, "test"));
    }

    [Fact]
    public void Quote_BracketsValidatedIdentifier()
    {
        Assert.Equal("[Posts]", SafeIdentifier.Quote("Posts"));
    }

    [Fact]
    public void Quote_Throws_ForInvalidIdentifier()
    {
        Assert.Throws<UnsafeIdentifierException>(() => SafeIdentifier.Quote("Posts; DROP TABLE x"));
    }

    [Fact]
    public void QualifiedName_ProducesSchemaQualifiedQuotedName()
    {
        Assert.Equal("[dbo].[Articles]", SafeIdentifier.QualifiedName("dbo", "Articles"));
    }

    [Fact]
    public void QualifiedName_Throws_ForInvalidTable()
    {
        Assert.Throws<UnsafeIdentifierException>(() => SafeIdentifier.QualifiedName("dbo", "Articles--"));
    }
}

using Publisher.Web.Services;

namespace Publisher.Tests;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Bài Viết Hấp Dẫn Số 1", "bai-viet-hap-dan-so-1")]
    [InlineData("Đường phố Hà Nội", "duong-pho-ha-noi")]
    [InlineData("Tiếng Việt có dấu", "tieng-viet-co-dau")]
    public void Generate_RemovesVietnameseDiacritics(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Theory]
    [InlineData("Hello    World", "hello-world")]
    [InlineData("a  b   c", "a-b-c")]
    public void Generate_CollapsesMultipleSpaces(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Theory]
    [InlineData("Hello, World!", "hello-world")]
    [InlineData("Price: $100 (USD)", "price-100-usd")]
    [InlineData("C# & .NET", "c-net")]
    public void Generate_StripsPunctuation(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Theory]
    [InlineData("   leading and trailing   ", "leading-and-trailing")]
    [InlineData("---dashes---", "dashes")]
    [InlineData("...", "")]
    public void Generate_TrimsLeadingTrailingDashes(string input, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_ReturnsEmpty_ForNullOrWhitespace(string? input)
    {
        Assert.Equal(string.Empty, SlugGenerator.Generate(input));
    }

    [Fact]
    public void Generate_KeepsDigitsAndLowercases()
    {
        Assert.Equal("post-2026-update", SlugGenerator.Generate("Post 2026 UPDATE"));
    }
}

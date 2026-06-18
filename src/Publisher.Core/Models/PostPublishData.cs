namespace Publisher.Core.Models;

/// <summary>
/// Flattened data the publisher needs from a <see cref="Entities.Post"/>.
/// Provided as a stable contract so the publisher (Agent E) does not need to take a
/// dependency on EF-tracked entities. The publisher MAY also accept a Post entity directly;
/// this DTO is the decoupled option.
/// </summary>
public sealed class PostPublishData
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? Thumbnail { get; set; }
    public string? CategoryId { get; set; }
    public string? AuthorId { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    /// <summary>JSON object of custom field values.</summary>
    public string? CustomDataJson { get; set; }
}

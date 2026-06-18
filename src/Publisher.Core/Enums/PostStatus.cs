namespace Publisher.Core.Enums;

/// <summary>
/// Logical post lifecycle states. NOTE: the database stores these as exact-case
/// lowercase strings ('draft' | 'scheduled' | 'publishing' | 'published' | 'failed').
/// Entity properties keep <see cref="string"/> to match the schema; use
/// <see cref="PostStatuses"/> for the canonical constants.
/// </summary>
public enum PostStatus
{
    Draft,
    Scheduled,
    Publishing,
    Published,
    Failed
}

/// <summary>
/// Canonical string constants for the Posts.Status column (CK_Posts_Status).
/// </summary>
public static class PostStatuses
{
    public const string Draft = "draft";
    public const string Scheduled = "scheduled";
    public const string Publishing = "publishing";
    public const string Published = "published";
    public const string Failed = "failed";

    public static readonly IReadOnlyList<string> All = new[] { Draft, Scheduled, Publishing, Published, Failed };
}

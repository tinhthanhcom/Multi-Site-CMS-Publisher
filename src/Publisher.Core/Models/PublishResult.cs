namespace Publisher.Core.Models;

/// <summary>Result of publishing a post to a remote site database.</summary>
public sealed class PublishResult
{
    public bool Success { get; set; }
    /// <summary>The identity/ID of the inserted row in the remote site DB (if any).</summary>
    public string? RemotePostId { get; set; }
    public string? Error { get; set; }
}

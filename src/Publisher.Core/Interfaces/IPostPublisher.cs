using Publisher.Core.Models;

namespace Publisher.Core.Interfaces;

/// <summary>Publishes a post to its target site database. Implemented later by Agent E.</summary>
public interface IPostPublisher
{
    /// <summary>Publishes the given post on behalf of the given user.</summary>
    Task<PublishResult> PublishAsync(int postId, int userId, CancellationToken ct = default);
}
